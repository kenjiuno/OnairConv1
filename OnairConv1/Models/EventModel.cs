using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OnairConv1.Models
{
    public class EventModel
    {
        [XmlElement] public string? d { get; set; }
        [XmlElement] public string? n { get; set; }
        [XmlElement] public string[]? l { get; set; }
        [XmlElement] public string[]? th { get; set; }
    }
}
