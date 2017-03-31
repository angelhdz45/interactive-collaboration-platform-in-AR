﻿using System.Collections.Generic;
using System.Linq;
using CollaborationEngine.Objects.Components;
using UnityEngine;
using UnityEngine.Networking;
using Object = UnityEngine.Object;

namespace CollaborationEngine.Objects
{
    public abstract class SceneObject
    {
        public class Data : MessageBase
        {
            public static uint SceneObjectCount;

            public uint ID;
            public Vector3 Position;
            public Quaternion Rotation;
            public Vector3 Scale;
            public SceneObjectType Type;
            public uint Flag;

            public Data Clone()
            {
                return (Data) MemberwiseClone();
            }
        };

        public class DataCollection : MessageBase
        {
            public IEnumerable<Data> DataEnumerable { get; set; }

            public override void Serialize(NetworkWriter writer)
            {
                writer.WritePackedUInt32((uint) DataEnumerable.Count());
                foreach (var data in DataEnumerable)
                {
                    writer.Write(data);
                }
            }

            public override void Deserialize(NetworkReader reader)
            {
                var count = reader.ReadPackedUInt32();

                var data = new List<Data>((int) count);
                for (var i = 0; i < count; ++i)
                    data.Add(reader.ReadMessage<Data>());

                DataEnumerable = data;
            }
        }

        public GameObject Prefab { get; set; }
        public GameObject GameObject { get; private set; }
        public bool IsInstanced
        {
            get { return _isInstanced; }
            private set { _isInstanced = value; }
        }
        public uint ID
        {
            get
            {
                lock (_networkData)
                {
                    return _networkData.ID;
                }
            }
        }
        public Vector3 Position
        {
            get
            {
                lock (_networkData)
                {
                    return _networkData.Position;
                }
            }
            set
            {
                lock (_networkData)
                {
                    _networkData.Position = value;
                    if (IsInstanced)
                        GameObject.transform.position = value;

                    IsDirty = true;
                }
            }
        }
        public uint Flag
        {
            get
            {
                lock (_networkData)
                {
                    return _networkData.Flag;
                }
            }
        }
        public Data NetworkData
        {
            get
            {
                lock (_networkData)
                {
                    return _networkData.Clone();
                }
            }
        }
        public bool IsDirty { get; set; }

        private readonly List<IComponent> _components = new List<IComponent>();
        public List<IComponent> Components
        {
            get
            {
                return _components;
            }
        }

        protected SceneObject(GameObject prefab, SceneObjectType type)
        {
            _networkData = new Data
            {
                Position = Vector3.zero,
                Rotation = Quaternion.identity,
                Scale = Vector3.one,
                Type = type
            };
            Prefab = prefab;
        }
        protected SceneObject(GameObject prefab, Data networkData)
        {
            _networkData = networkData;
            Prefab = prefab;
        }

        public virtual GameObject Instantiate(Transform parent)
        {
            GameObject = Object.Instantiate(Prefab, _networkData.Position, _networkData.Rotation);
            System.Diagnostics.Debug.Assert(GameObject != null, "GameObject != null");

            GameObject.transform.localScale = _networkData.Scale;
            GameObject.transform.SetParent(parent, false);
            foreach (var component in Components)
                component.Instantiate();

            IsInstanced = true;

            return GameObject;
        }
        public virtual void Destroy()
        {
            if (!IsInstanced)
                return;

            IsInstanced = false;

            ClearComponents();

            if (GameObject)
            {
                Object.Destroy(GameObject);
                GameObject = null;
            }
        }

        public virtual void FixedUpdate()
        {
            foreach (var component in Components)
                component.Update();
        }
        public virtual void FrameUpdate()
        {
        }

        public void AddComponent(IComponent component)
        {
            if (!Components.Exists(c => c == component))
            {
                Components.Add(component);

                if(_isInstanced)
                    component.Instantiate();
            }
                
        }
        public void RemoveComponent(IComponent component)
        {
            Components.Remove(component);

            if(_isInstanced)
                component.Destroy();
        }
        public void ClearComponents()
        {
            if (_isInstanced)
            {
                foreach (var component in Components)
                    component.Destroy();
            }
                
            Components.Clear();
        }

        public void UpdateTransform(Data data)
        {
            _networkData.Position = data.Position;
            _networkData.Rotation = data.Rotation;
            _networkData.Scale = data.Scale;

            GameObject.transform.position = data.Position;
            GameObject.transform.rotation = data.Rotation;
            GameObject.transform.localScale = data.Scale;
        }

        public TComponentType GetComponent<TComponentType>()
        {
            return GameObject.GetComponent<TComponentType>();
        }

        private bool _isInstanced;
        private readonly Data _networkData;
    }
}
