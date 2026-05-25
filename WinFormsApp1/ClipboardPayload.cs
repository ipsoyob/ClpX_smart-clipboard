using System;

namespace ManagerBuffer0
{
    public class ClipboardPayload
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; } = "TXT";
        public string Body { get; set; } = "";
        public string Meta { get; set; } = "";
        public string Alias { get; set; } = "";
    }
}
