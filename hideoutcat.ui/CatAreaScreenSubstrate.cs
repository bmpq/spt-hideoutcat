using EFT.UI;
using UnityEngine;

#if RUNTIME
using Comfort.Common;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Collections.Generic;
using System.Linq;
#endif

namespace tarkin.hideoutcat.ui
{
    public class CatAreaScreenSubstrate : UIElement
    {
        [SerializeField] private ItemSelectionCell cell;
#if RUNTIME
        private List<MongoID> _categoryFilter;

        public override void Display()
        {
            base.Display();

            if (_categoryFilter == null)
            {
                TemplateIdToObjectMappingsClass.BackwardTypeTable.TryGetValue(typeof(FoodItemClass), out string categoryId);
                _categoryFilter = GetAllItemIdsInCategory(categoryId);
            }

            cell.Show(null, new Func<Item, bool>(TestItem), new Action<Item>(SelectItem), _categoryFilter);
            cell.SetItemsSetAbility(true);
        }

        private List<MongoID> GetAllItemIdsInCategory(string categoryId)
        {
            var itemFactory = Singleton<ItemFactoryClass>.Instance;
            var categoryMongoId = new MongoID(categoryId);

            return itemFactory.ItemTemplates.Values
                .Where(template => template._type == NodeType.Item && IsDescendantOf(template, categoryMongoId, itemFactory))
                .Select(template => template._id)
                .ToList();
        }

        private bool IsDescendantOf(ItemTemplate itemToCheck, MongoID categoryId, ItemFactoryClass itemFactory)
        {
            MongoID? currentParentId = itemToCheck.ParentId;
            while (currentParentId.HasValue)
            {
                if (currentParentId.Value == categoryId)
                {
                    return true;
                }

                if (itemFactory.ItemTemplates.TryGetValue(currentParentId.Value, out ItemTemplate parentTemplate))
                {
                    currentParentId = parentTemplate.ParentId;
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        bool TestItem(Item itemToTest)
        {
            return true;
        }

        void SelectItem(Item selectedItem)
        {
            // placeholder for now

            if (selectedItem != null)
            {
                Debug.Log($"Item selected: {selectedItem.Name.Localized()}");
            }
            else
            {
                Debug.Log("Item selection cleared.");
            }
        }
#endif
    }
}
