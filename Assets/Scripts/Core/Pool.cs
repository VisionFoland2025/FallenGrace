using System;
using System.Collections.Generic;
using UnityEngine;

namespace FallenGrace
{
    /// <summary>
    /// Пул объектов: вместо Instantiate/Destroy в бою берём готовые объекты.
    /// Это главное, что держит FPS на телефонах при сотнях врагов.
    /// </summary>
    public class Pool<T> where T : Component
    {
        readonly Stack<T> _free = new();
        readonly Func<T> _create;

        public Pool(Func<T> create, int prewarm = 0)
        {
            _create = create;
            Prewarm(prewarm);
        }

        /// <summary>Заранее создать объекты, чтобы не было рывков в бою.</summary>
        public void Prewarm(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var item = _create();
                item.gameObject.SetActive(false);
                _free.Push(item);
            }
        }

        public T Get()
        {
            var item = _free.Count > 0 ? _free.Pop() : _create();
            item.gameObject.SetActive(true);
            return item;
        }

        public void Release(T item)
        {
            if (!item.gameObject.activeSelf) return;
            item.gameObject.SetActive(false);
            _free.Push(item);
        }
    }
}
