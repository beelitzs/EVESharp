using IniParser.Model;

namespace EVESharp.Node.Configuration
{
    public class Proxy
    {
        public string Hostname { get; set; }
        public ushort Port { get; set; }

        public void Load(KeyDataCollection section)
        {
            this.Hostname = section["hostname"];
            this.Port = ushort.Parse(section["port"]);
        }
    }
}