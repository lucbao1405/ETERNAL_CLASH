using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace EternalClash.UI
{
    [Serializable]
    public class TownGuideData
    {
        public string guideName;
        public Transform target;
        public Image icon;
        public UnityEvent onArrive;
    }
}
