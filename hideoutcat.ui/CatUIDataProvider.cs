using System;
using System.Collections.Generic;
using UnityEngine;

namespace tarkin.hideoutcat.ui
{
    public static class CatUIDataProvider
    {
        private static Func<Coat[]> _coatsLoader;
        private static Func<Coat> _currentCoatLoader;
        private static Action<Coat> _coatApplier;

        private static List<Action> _readyCallbacks = new List<Action>();

        public static bool IsReady { get; private set; }

        public static void Initialize(Func<Coat[]> coatsLoader, Func<Coat> currentCoatLoader, Action<Coat> coatApplier)
        {
            if (IsReady) return;

            _coatsLoader = coatsLoader ?? throw new ArgumentNullException(nameof(coatsLoader));
            _currentCoatLoader = currentCoatLoader ?? throw new ArgumentNullException(nameof(currentCoatLoader));
            _coatApplier = coatApplier ?? throw new ArgumentNullException(nameof(coatApplier));

            IsReady = true;

            foreach (var callback in _readyCallbacks)
            {
                callback?.Invoke();
            }
            _readyCallbacks.Clear();

            Debug.Log("CatDataProvider is now initialized and ready.");
        }

        public static void WhenReady(Action onReady)
        {
            if (IsReady)
            {
                onReady?.Invoke();
            }
            else
            {
                _readyCallbacks.Add(onReady);
            }
        }

        public static Coat[] GetCoats()
        {
            if (!IsReady)
            {
                Debug.LogError("GetCoats() called before CatDataProvider was ready!");
                return Array.Empty<Coat>();
            }
            return _coatsLoader();
        }

        public static Coat GetCurrentCoat()
        {
            if (!IsReady)
            {
                Debug.LogError("GetCurrentCoat() called before CatDataProvider was ready!");
                return null;
            }
            return _currentCoatLoader();
        }

        public static void ApplyCoat(Coat coat)
        {
            if (!IsReady)
            {
                Debug.LogError("ApplyCoat() called before CatDataProvider was ready!");
                return;
            }
            _coatApplier(coat);
        }

        public static void Reset()
        {
            IsReady = false;
            _coatsLoader = null;
            _currentCoatLoader = null;
            _coatApplier = null;
            _readyCallbacks.Clear();
        }
    }
}