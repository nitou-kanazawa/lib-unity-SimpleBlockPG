using System;
using System.Linq;
using System.Xml.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace nitou.BlockPG.Serialization {

    // [NOTE] 元の手続き的コードは拡張が難しかったため、OOPを意識して書き直している．

    /// <summary>
    /// BlockEngin標準の<see cref="BE2_SerializableBlock"/>をカスタムした独自クラス．
    /// </summary>
    [Serializable]
    public sealed class SerializableBlock {

        public int id;
        public string name;
        public Vector3 localPosition;
        public List<SerializableBlockSection> sections;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Constructor.
        /// </summary>
        public SerializableBlock(int id, string name, Vector3 localPosition) {
            // Block情報
            this.id = id;
            this.name = name;
            this.localPosition = localPosition;

            // 子孫情報
            this.sections = new List<SerializableBlockSection>();
        }


        /// ----------------------------------------------------------------------------
        #region Static Method (データ変換)

        /// <summary>
        /// Identification key.
        /// </summary>
        public static readonly string NAME_KEY = "Block";

        /// <summary>
        /// Converting from classes to XML elements for serialization.
        /// </summary>
        public static XElement ToXElement(SerializableBlock sBlock) {

            var xBlock = new XElement(NAME_KEY);
            {
                // params
                xBlock.Add(new XElement("id", sBlock.id));
                xBlock.Add(new XElement("name", sBlock.name));
                xBlock.Add(new XElement("localPosition", XmlUtils.Vector3ToString(sBlock.localPosition)));

                // section list
                var xSections = new XElement("sections");
                xSections.Add(sBlock.sections.Select(sSection => SerializableBlockSection.ToXElement(sSection)));
                xBlock.Add(xSections);
            }
            return xBlock;
        }

        /// <summary>
        /// Converting from XML elements to classes for serialization.
        /// </summary>
        public static SerializableBlock FromXElement(XElement xBlock) {

            var sBlock = new SerializableBlock(
                id: int.TryParse(xBlock.Element("id").Value, out var id) ? id : -1,
                name: xBlock.Element("name").Value,
                localPosition: XmlUtils.StringToVector3(xBlock.Element("localPosition").Value));

            // section List
            var xSections = xBlock.Element("sections").Elements(SerializableBlockSection.NAME_KEY);
            sBlock.sections.AddRange(xSections.Select(xSection => SerializableBlockSection.FromXElement(xSection)));

            return sBlock;
        }
        #endregion
    }

}