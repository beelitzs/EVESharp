using System.Collections;
using System.Collections.Generic;
using EVESharp.Node.StaticData.Inventory;
using EVESharp.PythonTypes.Types.Collections;

namespace EVESharp.Node.Inventory.Items.Attributes
{
    public class AttributeList : IEnumerable
    {
        private readonly ItemFactory mItemFactory = null;

        private readonly Dictionary<int, Attribute> mDefaultAttributes = null;
        private readonly Dictionary<int, Attribute> mItemAttributes = null;
        
        public AttributeList(ItemFactory factory, Type type, Dictionary<int, Attribute> attributes)
        {
            this.mItemFactory = factory;
            
            // load the default attributes list
            this.mDefaultAttributes = type.Attributes;
            // load item attributes
            this.mItemAttributes = attributes;
        }

        public Attribute this[int index]
        {
            get
            {
                if (this.mItemAttributes.TryGetValue(index, out Attribute attrib) == true)
                    return attrib;
                if (this.mDefaultAttributes.TryGetValue(index, out Attribute defAttrib) == true)
                    return this.mItemAttributes[index] = defAttrib;
                
                return this.mItemAttributes[index] = new Attribute(this.mItemFactory.AttributeManager[index], 0, true);
            }

            set
            {
                this.mItemAttributes[index] = value;
            }
        }

        public Attribute this[long index]
        {
            get => this[(int) index];
            set => this[(int) index] = value;
        }

        public Attribute this[StaticData.Inventory.Attributes index]
        {
            get => this[(int) index];
            set => this[(int) index] = value;
        }

        public bool TryGetAttribute(StaticData.Inventory.Attributes index, out Attribute attrib)
        {
            return this.mItemAttributes.TryGetValue((int) index, out attrib) || this.mDefaultAttributes.TryGetValue((int) index, out attrib);
        }

        public bool AttributeExists(Attribute attribute)
        {
            return this.AttributeExists(attribute.Info.ID);
        }

        public bool AttributeExists(int attributeID)
        {
            return this.mItemAttributes.ContainsKey(attributeID) || this.mDefaultAttributes.ContainsKey(attributeID);
        }

        public bool AttributeExists(StaticData.Inventory.Attributes attributeID)
        {
            return this.AttributeExists((int) attributeID);
        }
        
        public void Persist(ItemEntity item)
        {
            this.mItemFactory.ItemDB.PersistAttributeList(item, this);
        }
        
        public IEnumerator GetEnumerator()
        {
            return this.mItemAttributes.GetEnumerator();
        }

        public void MergeFrom(AttributeList list)
        {
            foreach (Attribute attrib in list)
                this[attrib.Info.ID] = attrib.Clone();
        }

        public void MergeInto(AttributeList list)
        {
            foreach (Attribute attrib in this)
                list[attrib.Info.ID] = attrib.Clone();
        }

        public static implicit operator PyDictionary(AttributeList list)
        {
            PyDictionary result = new PyDictionary();

            foreach (KeyValuePair<int, Attribute> attrib in list.mDefaultAttributes)
                result[attrib.Key] = attrib.Value;
            
            foreach (KeyValuePair<int, Attribute> attrib in list.mItemAttributes)
                result[attrib.Key] = attrib.Value;

            return result;
        }
    }
}