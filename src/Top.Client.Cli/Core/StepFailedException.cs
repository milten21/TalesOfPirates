using System;

namespace Top.Client.Cli.Core;

public class StepFailedException(string reason) : Exception(reason);
