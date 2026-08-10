using System;
using System.Xml.Linq;
using UnityEngine;

namespace nitou.BlockPG.Serialization {

    [Serializable]
    public sealed class SerializableInput {

        public readonly string value;


        /// ----------------------------------------------------------------------------
        // Public Method

        /// <summary>
        /// Constructor.
        /// </summary>
        public SerializableInput(string value) {
            this.value = value;
        }


        /// ----------------------------------------------------------------------------
        #region Static Method (Data conversion)

        /// <summary>
        /// 識別名.
        /// </summary>
        public static readonly string NAME_KEY = "Input";

        /// <summary>
        /// Converting from classes to XML elements for serialization.
        /// </summary>
        public static XElement ToXElement(SerializableInput sInput) {
            var xInput = new XElement(NAME_KEY,
                new XElement("value", sInput.value)
            );
            return xInput;
        }

        /// <summary>
        /// Converting from XML elements to classes for serialization.
        /// </summary>
        public static SerializableInput FromXElement(XElement xInput) {
            if (xInput == null) {
                Debug.LogWarning("XElement is null. Returning default SerializableInput.");
                return new SerializableInput(string.Empty);
            }

            var value = xInput.Element("value")?.Value ?? string.Empty;
            return new SerializableInput(value);
        }
        #endregion
    }

}