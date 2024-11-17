using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace OnairConv1.Models
{
    [XmlRoot("onairconv1")]
    public class Onairconv1Model
    {
        /// <summary>
        /// page title
        /// </summary>
        [XmlAttribute] public string? title { get; set; }

        /// <summary>
        /// events
        /// </summary>
        [XmlElement] public EventModel[]? e { get; set; }

    }
}
