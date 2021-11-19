namespace dtmi_rido
{
    public enum DiagnosticsMode
    {
        minimal = 0,
        complete = 1,
        full = 2
    }

    public class Cmd_getRuntimeStats_Request
    {
        public DiagnosticsMode DiagnosticsMode { get; set; }
    }
}
