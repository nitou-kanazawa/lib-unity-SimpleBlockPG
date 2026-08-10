using System;
using System.Linq;
using System.Collections.Generic;
using System.Xml.Linq;

namespace nitou.BlockPG.Serialization {

    public sealed class SerializableBlockSection {

        public readonly List<SerializableBlock> childBlocks;
        public readonly List<SerializableInput> inputs;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Constructor.
        /// </summary>
        public SerializableBlockSection() {
            childBlocks = new List<SerializableBlock>();
            inputs = new List<SerializableInput>();
        }


        /// ----------------------------------------------------------------------------
        #region Static Method (Data conversion)

        /// <summary>
        /// Identification key.
        /// </summary>
        public static readonly string NAME_KEY = "Section";

        /// <summary>
        /// Converting from classes to XML elements for serialization.
        /// </summary>
        public static XElement ToXElement(SerializableBlockSection sSection) {
            var xSection = new XElement(NAME_KEY);
            {
                // Block List
                var xChildBlocks = new XElement("childBlocks");
                sSection.childBlocks.ForEach(block => xChildBlocks.Add(SerializableBlock.ToXElement(block)));
                xSection.Add(xChildBlocks);

                // Input List
                var xInputs = new XElement("inputs");
                sSection.inputs.ForEach(input => xInputs.Add(SerializableInput.ToXElement(input)));
                xSection.Add(xInputs);
            }
            return xSection;
        }

        /// <summary>
        /// Converting from XML elements to classes for serialization.
        /// </summary>
        public static SerializableBlockSection FromXElement(XElement xSection) {
            if (xSection == null)
                throw new ArgumentNullException(nameof(xSection));

            var sSection = new SerializableBlockSection();
            {
                // [NOTE] 欠落した要素があっても復元を継続できるよう、要素の取得は null 許容で行う．

                // Block list
                var xChildBlocks = xSection.Element("childBlocks")?.Elements(SerializableBlock.NAME_KEY);
                if (xChildBlocks != null) {
                    sSection.childBlocks.AddRange(xChildBlocks.Select(xBlock => SerializableBlock.FromXElement(xBlock)));
                }

                // Input list
                var xInputs = xSection.Element("inputs")?.Elements(SerializableInput.NAME_KEY);
                if (xInputs != null) {
                    sSection.inputs.AddRange(xInputs.Select(xInput => SerializableInput.FromXElement(xInput)));
                }
            }
            return sSection;
        }
        #endregion
    }

}