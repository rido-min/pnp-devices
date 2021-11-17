namespace dtmi_rido;

public class Cmd_getRuntimeStats_Response : Dictionary<string, string>
{
    public int? _status { get; set; }
    public int? _rid { get; set; }
}
