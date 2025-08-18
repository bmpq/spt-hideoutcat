using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace tarkin.hideoutcat.ui.Generic
{
    public class MyTabGroup
    {
        private readonly List<MyTab> _managedTabs = new List<MyTab>();
        public IReadOnlyList<MyTab> ManagedTabs => _managedTabs;

        public MyTab CurrentSelectedTab { get; private set; }
        public event Action<MyTab> OnTabSelected;
        internal void OnTabClicked(MyTab tab)
        {
            SelectTab(tab, true);
        }

        public void AddTab(MyTab tab)
        {
            if (tab == null || _managedTabs.Contains(tab)) return;

            _managedTabs.Add(tab);
            tab.Initialize(this);

            if (CurrentSelectedTab == null && tab.IsInteractable)
            {
                SelectTab(tab, true);
            }
            else
            {
                tab.Deselect();
            }
        }

        public void RemoveTab(MyTab tab)
        {
            if (tab == null || !_managedTabs.Contains(tab)) return;

            bool wasSelected = (tab == CurrentSelectedTab);
            _managedTabs.Remove(tab);

            if (wasSelected)
            {
                CurrentSelectedTab = null;
                SelectTab(_managedTabs.FirstOrDefault(t => t.IsInteractable), true);
            }
        }

        public void ClearTabs(bool destroyTabObjects = true)
        {
            if (destroyTabObjects)
            {
                // create a copy to iterate over while destroying
                foreach (var tab in new List<MyTab>(_managedTabs))
                {
                    if (tab != null && tab.gameObject != null)
                    {
                        GameObject.Destroy(tab.gameObject);
                    }
                }
            }

            _managedTabs.Clear();
            CurrentSelectedTab = null;
        }

        public void SelectTab(MyTab tabToSelect, bool sendCallback = true)
        {
            if (tabToSelect == null || !tabToSelect.IsInteractable || CurrentSelectedTab == tabToSelect)
            {
                return;
            }

            if (CurrentSelectedTab != null)
            {
                CurrentSelectedTab.Deselect();
            }

            CurrentSelectedTab = tabToSelect;
            CurrentSelectedTab.Select();

            if (sendCallback)
            {
                OnTabSelected?.Invoke(CurrentSelectedTab);
            }
        }

        public void SelectTab(int index, bool sendCallback = true)
        {
            if (index >= 0 && index < _managedTabs.Count)
            {
                SelectTab(_managedTabs[index], sendCallback);
            }
            else
            {
                Debug.LogWarning($"Cat Tab index {index} is out of range.");
            }
        }
    }
}
