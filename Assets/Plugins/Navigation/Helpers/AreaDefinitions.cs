using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Unity.Collections;

namespace Providers.Grid
{
    public interface INativeArrayProducer<T> where T : struct
    {
        NativeArray<T> ToNativeArray(Allocator allocator);
    }

    [Serializable]
    public class AreaDefinitions : INativeArrayProducer<NativeAreaDefinition>
    {
        public virtual IDictionary<Enum, AreaDefinition> Items { get; }

        public NativeArray<NativeAreaDefinition> ToNativeArray(Allocator allocator)
        {
            var nativeItems = Items.Values.Select(v => new NativeAreaDefinition
            {
                Index = v.Index,
                Weight = v.Weight,
                FlagValue = v.FlagValue

            }).ToArray();

            return new NativeArray<NativeAreaDefinition>(nativeItems, allocator);
        }
    }

    public class AreaDefinitions<T> : AreaDefinitions, IEnumerable<AreaDefinition<T>> where T : struct, Enum
    {
        public AreaDefinitions()
        {
            Items = ((T[])Enum.GetValues(typeof(T)))
                .ToDictionary(k => (Enum)k, v => (AreaDefinition)AreaDefinition.FromFlag(v, 0f));
        }

        public AreaDefinition<T> this[T flag] => Items[flag] as AreaDefinition<T>;
        public AreaDefinition<T> this[int i] => Items.ElementAtOrDefault(i).Value as AreaDefinition<T>;

        public void Add(T flag, float value) // for collection initializer
        {
            Items[flag].Weight = value;
        }

        public void SetWeight(T flag, float value)
        { 
            Items[flag].Weight = value;
        }

        public void SetWeight(IEnumerable<KeyValuePair<T,float>> weights)
        {
            foreach (var pair in weights)
            {
                Items[pair.Key].Weight = pair.Value;
            }
        }

        public override IDictionary<Enum, AreaDefinition> Items { get; }

        public IEnumerator<AreaDefinition<T>> GetEnumerator() => Items.Values.OfType<AreaDefinition<T>>().GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public class AreaDefinition<T> : AreaDefinition where T : Enum
    {
        public T Flag { get; set; }
    }

    [Serializable]
    public class AreaDefinition
    {
        public static AreaDefinition<T> FromFlag<T>(T flag, float weight) where T : Enum
        {
            var value = Convert.ToUInt64(flag);
            return new AreaDefinition<T>
            {
                Weight = weight,
                Name = flag.ToString(),
                Index = (int)Math.Log(value, 2),
                FlagValue = value,
                Flag = flag,
            };
        }

        public ulong FlagValue;
        public string Name;
        public int Index;
        public float Weight;
    }

    [Serializable]
    public struct NativeAreaDefinition
    {
        public int Index;
        public float Weight;
        public ulong FlagValue;
    }
}