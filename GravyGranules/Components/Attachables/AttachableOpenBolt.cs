using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using FistVR;
using UnityEngine;
using GravyScripts.Components;
using System.Reflection;

namespace Plugin.Components
{
    public class AttachableOpenBolt : AttachableOpenBoltReceiver
    {
        [ContextMenu("Convert to New")]
        void TranslatePrefab()
        {
            AttachableOpenBoltReceiver newComponent = gameObject.AddComponent<AttachableOpenBoltReceiver>();
            foreach (FieldInfo field in typeof(AttachableOpenBoltReceiver).GetFields())
            {
                field.SetValue(newComponent, field.GetValue(this));
            }
        }
    }
}
