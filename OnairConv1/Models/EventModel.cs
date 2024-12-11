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
        /// <summary>
        /// date of the event
        /// </summary>
        [XmlElement] public string? d { get; set; }

        /// <summary>
        /// name of the event
        /// </summary>
        [XmlElement] public string? n { get; set; }

        /// <summary>
        /// participation type
        /// </summary>
        [XmlElement] public string[]? pt { get; set; }

        /// <summary>
        /// markdown'ed links
        /// </summary>
        [XmlElement] public string[]? l { get; set; }

        /// <summary>
        /// twitter hashtags `#hash`
        /// </summary>
        [XmlElement] public string[]? th { get; set; }

        /// <summary>
        /// 物理 開催場所
        /// </summary>
        [XmlElement] public string[]? pl { get; set; }

        /// <summary>
        /// 公開終了日時
        /// </summary>
        [XmlElement] public string? ed { get; set; }

    }
}
