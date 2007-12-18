using System;
using System.Reflection;
using System.Xml;

using NAnt.Core;
using NAnt.Core.Attributes;

namespace NAnt.Core.Configuration {
    [Serializable]
    internal class ConditionalElement : Element {
        #region Private Instance Fields

        private bool _ifDefined = true;
        private bool _unlessDefined;

        #endregion Private Instance Fields

        #region Protected Instance Constructors

        protected override void InitializeXml(XmlNode elementNode, PropertyDictionary properties, FrameworkInfo framework) {
            XmlNode = elementNode;

            ConditionalConfigurator configurator = new ConditionalConfigurator(
                this, elementNode, properties, framework);
            configurator.Initialize();
        }

        #endregion Protected Instance Constructors

        #region Protected Instance Properties

        [TaskAttribute("if")]
        protected bool IfDefined {
            get { return _ifDefined; }
            set { _ifDefined = value; }
        }

        [TaskAttribute("unless")]
        protected bool UnlessDefined {
            get { return _unlessDefined; }
            set { _unlessDefined = value; }
        }

        #endregion Protected Instance Properties

        #region Internal Instance Properties

        internal bool Enabled {
            get {
                return IfDefined && !UnlessDefined;
            }
        }

        #endregion Internal Instance Properties

        #region Override implementation of Element

        #endregion Override implementation of Element

        class ConditionalConfigurator : AttributeConfigurator {
            public ConditionalConfigurator(ConditionalElement element, XmlNode elementNode, PropertyDictionary properties, FrameworkInfo targetFramework) :
                base (element, elementNode, properties, targetFramework) {
                Type currentType = element.GetType();

                PropertyInfo ifdefined = currentType.GetProperty("IfDefined",
                    BindingFlags.NonPublic | BindingFlags.Instance);
                InitializeAttribute(ifdefined);

                if (!element.IfDefined) {
                    _enabled = false;
                } else {
                    PropertyInfo unlessDefined = currentType.GetProperty(
                        "UnlessDefined",
                        BindingFlags.NonPublic | BindingFlags.Instance);
                    InitializeAttribute(unlessDefined);
                    _enabled = !element.UnlessDefined;
                }

                if (!_enabled) {
                    // since we will not be processing other attributes or
                    // child nodes, clear these collections to avoid
                    // errors for unrecognized attributes/elements
                    UnprocessedAttributes.Clear();
                    UnprocessedChildNodes.Clear();
                }
            }

            protected override bool InitializeAttribute(PropertyInfo propertyInfo) {
                if (!_enabled)
                    return true;
                return base.InitializeAttribute (propertyInfo);
            }

            protected override void InitializeOrderedChildElements() {
                if (!_enabled)
                    return;
                base.InitializeOrderedChildElements ();
            }

            private readonly bool _enabled = true;
        }
    }
}
