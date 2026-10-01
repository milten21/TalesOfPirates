using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Top.Client.Features.MainMenu.Servers.Widgets;
using Top.Client.UI;
using Top.Logging;

namespace Top.Client.Features.MainMenu.Servers;

public partial class ServersScreen : Screen
{
    private const int MaximumServersWithoutFilter = 6;

    private readonly ButtonGroup _rowGroup = new();

    private readonly List<ServerRow> _rows = [];

    private readonly List<ServerRow> _shownRows = [];

    private ServerStore _serverStore = null!;

    private ServerEntry? _selected;

    [Signal]
    public delegate void ServerChosenEventHandler();

    [Export] public PackedScene ServerRowScene { get; set; } = null!;

    [Export] public StepBlock Block { get; set; } = null!;

    [Export] public Control FilterBar { get; set; } = null!;

    [Export] public LineEdit FilterEdit { get; set; } = null!;

    [Export] public Button ClearFilterButton { get; set; } = null!;

    [Export] public ScrollList ServerList { get; set; } = null!;

    [Export] public Label ListHint { get; set; } = null!;

    [Export] public ExpandContainer AddRowSlot { get; set; } = null!;

    [Export] public Button AddRow { get; set; } = null!;

    [Export] public ServerForm Form { get; set; } = null!;

    [Export] public Button BackButton { get; set; } = null!;

    [Export] public Button JoinButton { get; set; } = null!;

    [Export] public Button CancelButton { get; set; } = null!;

    [Export] public Button SaveButton { get; set; } = null!;

    public ServerEntry? ChosenServer { get; private set; }

    protected override Control NavigationStart => _shownRows.Count > 0 ? _shownRows[0].Row : AddRow;

    private string Query => FilterEdit.Text.Trim();

    public void Setup(ServerStore serverStore)
    {
        _serverStore = serverStore;
    }

    public override void _Ready()
    {
        FilterEdit.TextChanged += _ => ChangeFilter();
        FilterEdit.TextSubmitted += _ => FocusRowOrAddRow(0);
        ClearFilterButton.Pressed += ClearFilter;

        AddRow.Pressed += OpenAdd;
        Form.Changed += UpdateSaveButton;
        Form.Submitted += SaveServer;

        BackButton.Pressed += Close;
        JoinButton.Pressed += JoinSelected;
        CancelButton.Pressed += CancelForm;
        SaveButton.Pressed += SaveServer;

        Block.AcceptRequested += Accept;
        Block.CancelRequested += CancelOrGoBack;

        ShowServers();
        ShowActions();
    }

    private void ShowServers()
    {
        foreach (ServerRow row in _rows)
        {
            row.Visible = false;
            row.QueueFree();
        }

        _rows.Clear();

        if (_selected is not null && !_serverStore.Entries.Contains(_selected))
        {
            _selected = null;
        }

        DateTime now = DateTime.Now;

        foreach (ServerEntry entry in _serverStore.Entries)
        {
            _rows.Add(AddServerRow(entry, now));
        }

        FilterBar.Visible = _rows.Count > MaximumServersWithoutFilter;

        if (!FilterBar.Visible)
        {
            FilterEdit.Text = "";
        }

        UpdateJoinButton();
        ShowFilteredServers();
    }

    private ServerRow AddServerRow(ServerEntry entry, DateTime now)
    {
        var row = ServerRowScene.Instantiate<ServerRow>();

        row.Setup(entry, _rowGroup, now);

        if (entry == _selected)
        {
            row.Select();
        }

        ServerList.Items.AddChild(row);

        row.Selected += () => Select(entry);
        row.Activated += () => Join(entry);
        row.EditPressed += () => OpenEdit(row);
        row.RemovePressed += () => AskToRemove(row);
        row.Confirmed += () => Remove(entry, row.WasConfirmOpenedWithKeyboard);

        return row;
    }

    private void ShowFilteredServers()
    {
        string query = Query;

        _shownRows.Clear();

        foreach (ServerRow row in _rows)
        {
            row.Visible = Matches(row.Entry, query);

            if (row.Visible)
            {
                _shownRows.Add(row);
            }
            else
            {
                row.CloseConfirm();
            }
        }

        ServerList.Visible = _shownRows.Count > 0;

        ClearFilterButton.Visible = FilterEdit.Text.Length > 0;

        ListHint.Visible = _shownRows.Count == 0;
        ListHint.Text = _rows.Count == 0
            ? "No servers yet. Add one to play online."
            : $"No saved server matches “{query}”.";

        Block.ShowSubtitle(query.Length == 0
            ? $"{_rows.Count} saved"
            : $"{_shownRows.Count} of {_rows.Count} saved");

        SetupFocus();
    }

    private void ChangeFilter()
    {
        ShowFilteredServers();

        ServerList.ScrollToTop();
    }

    private void ClearFilter()
    {
        FilterEdit.Text = "";
        FilterEdit.GrabFocus();

        ChangeFilter();
    }

    private void Select(ServerEntry entry)
    {
        _selected = entry;

        CloseConfirmsExcept(entry);
        UpdateJoinButton();
    }

    private void CloseConfirmsExcept(ServerEntry? entry)
    {
        foreach (ServerRow row in _rows.Where(row => row.Entry != entry))
        {
            row.CloseConfirm();
        }
    }

    private void UpdateJoinButton()
    {
        JoinButton.Disabled = _selected is null;
    }

    private void UpdateSaveButton()
    {
        SaveButton.Disabled = !Form.IsValid;
    }

    private void OpenAdd()
    {
        CloseConfirmsExcept(null);

        Form.OpenToAdd();

        ShowActions();
    }

    private void OpenEdit(ServerRow row)
    {
        row.Select();

        CloseConfirmsExcept(null);

        Form.OpenToEdit(row.Entry);

        ShowActions();
    }

    private void CancelForm()
    {
        ServerEntry? edited = Form.EditedEntry;

        CloseForm();

        FocusRowOrAddRow(edited is null ? -1 : FindShownRowIndex(edited));
    }

    private void SaveServer()
    {
        if (!Form.IsExpanded || !Form.TryRead(out string? name, out string? host, out int port))
        {
            return;
        }

        ServerEntry? entry = Form.EditedEntry;

        if (entry is null)
        {
            entry = _serverStore.Add(name, host, port);
        }
        else
        {
            _serverStore.Edit(entry, name, host, port);
        }

        _selected = entry;

        if (!Matches(entry, Query))
        {
            FilterEdit.Text = "";
        }

        CloseForm();
        ShowServers();

        FocusRowOrAddRow(FindShownRowIndex(entry));
    }

    private void AskToRemove(ServerRow row)
    {
        CloseForm();
        CloseConfirmsExcept(row.Entry);

        row.AskToRemove();
    }

    private void Remove(ServerEntry entry, bool shouldFocusNeighbour)
    {
        int index = FindShownRowIndex(entry);
        ServerEntry? neighbour = (_shownRows.ElementAtOrDefault(index + 1) ?? _shownRows.ElementAtOrDefault(index - 1))?.Entry;

        _serverStore.Remove(entry);

        _selected = neighbour;

        ShowServers();

        if (shouldFocusNeighbour)
        {
            FocusRowOrAddRow(neighbour is null ? -1 : FindShownRowIndex(neighbour));
        }
    }

    private void CloseForm()
    {
        Form.Close();

        ShowActions();
    }

    private void Accept()
    {
        if (Form.IsExpanded)
        {
            SaveServer();
        }
        else if (!_rows.Any(row => row.IsConfirming))
        {
            JoinSelected();
        }
    }

    private void CancelOrGoBack()
    {
        if (Form.IsExpanded)
        {
            CancelForm();
        }
        else if (_rows.FirstOrDefault(row => row.IsConfirming) is { } row)
        {
            row.Keep();
        }
        else
        {
            Close();
        }
    }

    private void JoinSelected()
    {
        if (_selected is { } entry)
        {
            Join(entry);
        }
    }

    private void Join(ServerEntry entry)
    {
        if (Form.IsExpanded)
        {
            return;
        }

        ChosenServer = entry;

        Log.Info($"chose {entry.Name} at {entry.Address}");

        EmitSignal(SignalName.ServerChosen);
    }

    private int FindShownRowIndex(ServerEntry entry)
    {
        return _shownRows.FindIndex(row => row.Entry == entry);
    }

    private void FocusRowOrAddRow(int index)
    {
        if (index < 0 || index >= _shownRows.Count)
        {
            AddRow.GrabFocus();

            return;
        }

        _shownRows[index].Row.GrabFocus();
    }

    private void ShowActions()
    {
        bool isFormOpen = Form.IsExpanded;

        if (isFormOpen)
        {
            AddRowSlot.Collapse();
        }
        else
        {
            AddRowSlot.Expand();
        }

        BackButton.Visible = !isFormOpen;
        JoinButton.Visible = !isFormOpen;
        CancelButton.Visible = isFormOpen;
        SaveButton.Visible = isFormOpen;

        UpdateSaveButton();
        UpdateJoinButton();
        SetupFocus();
    }

    private void SetupFocus()
    {
        List<Control> order = [];

        Control? above = FilterBar.Visible ? FilterEdit : null;
        Control below = Form.IsExpanded ? Form.NameEdit : AddRow;
        Control? firstRow = _shownRows.Count > 0 ? _shownRows[0].Row : null;

        if (FilterBar.Visible)
        {
            LinkVertically(FilterEdit, null, firstRow ?? below);
            LinkVertically(ClearFilterButton, null, firstRow ?? below);
            LinkHorizontally(FilterEdit, ClearFilterButton);

            order.Add(FilterEdit);

            if (ClearFilterButton.Visible)
            {
                order.Add(ClearFilterButton);
            }
        }

        for (int index = 0; index < _shownRows.Count; index++)
        {
            ServerRow row = _shownRows[index];
            ServerRow? upper = index > 0 ? _shownRows[index - 1] : null;
            ServerRow? lower = index < _shownRows.Count - 1 ? _shownRows[index + 1] : null;

            LinkVertically(row.Row, upper?.Row ?? above, lower?.Row ?? below);
            LinkVertically(row.EditButton, upper?.EditButton ?? above, lower?.EditButton ?? below);
            LinkVertically(row.RemoveButton, upper?.RemoveButton ?? above, lower?.RemoveButton ?? below);

            LinkVertically(row.InlineConfirm.ConfirmButton, row.Row, lower?.Row ?? below);
            LinkVertically(row.InlineConfirm.KeepButton, row.Row, lower?.Row ?? below);

            order.AddRange([row.Row, row.EditButton, row.RemoveButton]);
        }

        Control? lastRow = _shownRows.Count > 0 ? _shownRows[^1].Row : above;

        if (Form.IsExpanded)
        {
            Form.NameEdit.FocusNeighborTop = lastRow is null ? new NodePath() : Form.NameEdit.GetPathTo(lastRow);
            Form.HostEdit.FocusNeighborBottom = Form.HostEdit.GetPathTo(SaveButton);
            Form.PortEdit.FocusNeighborBottom = Form.PortEdit.GetPathTo(SaveButton);
            LinkVertically(CancelButton, Form.HostEdit, null);
            LinkVertically(SaveButton, Form.PortEdit, null);
            LinkHorizontally(CancelButton, SaveButton);

            order.AddRange([Form.NameEdit, Form.HostEdit, Form.PortEdit, CancelButton, SaveButton]);
        }
        else
        {
            LinkVertically(AddRow, lastRow, JoinButton);
            LinkVertically(BackButton, AddRow, null);
            LinkVertically(JoinButton, AddRow, null);
            LinkHorizontally(BackButton, JoinButton);

            order.AddRange([AddRow, BackButton, JoinButton]);
        }

        ChainFocus([.. order]);
    }

    private static bool Matches(ServerEntry entry, string query)
    {
        return entry.Name.Contains(query, StringComparison.OrdinalIgnoreCase) ||
               entry.Address.Contains(query, StringComparison.OrdinalIgnoreCase);
    }
}
