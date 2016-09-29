using System;
using System.IO;
#if !UNITY_WP_8_1
using System.Runtime.Serialization.Formatters.Binary;
#endif
using System.Xml.Serialization;
using UnityEngine;

namespace Utilities
{
    public enum SerializerType
    {
        XML,
        BINARY,
    }

    public static class Saving
    {
        public static string Path
        {
            get
            {
                return "";
            }
        }
        private static Serializer<Class> GetSerializer<Class>(SerializerType type)
        {
            switch (type)
            {
                case SerializerType.BINARY:
#if !UNITY_WP_8_1
                    return new MyBinaryFormatter<Class>();
#else
                    Debug.LogError("Can't use binary Formatter on Windows phone");
                    return new MyXmlSerializer<Class>();
#endif
                case SerializerType.XML:
                    return new MyXmlSerializer<Class>();
                default:
                    Debug.LogError("There is no serializer corresponding to this enum type : " + type);
                    return null;
            }
        }

        public static void Save<Class>(string path, Class objectToSerialise)
        {
            Save(path, objectToSerialise, SerializerType.XML);
        }

        public static void Save<Class>(string path, Class objectToSerialise, SerializerType type)
        {
            Serializer<Class> serializer = GetSerializer<Class>(type);
            path = GetPath(path, serializer);
            FileStream file = File.Create(path);

            serializer.Serialize(file, objectToSerialise);
            file.Close();
            Debug.Log("Saved to " + path);
        }

        public static bool TryLoad<SubClass, Class>(string path, out Class value, bool fromResources, SerializerType type = SerializerType.XML) where SubClass : Class
        {
            Serializer<SubClass> serializer = GetSerializer<SubClass>(type);
            path = GetPath(path, serializer);
            Debug.Log("Loading : " + path);
            if (!fromResources && File.Exists(path))
            {
                FileStream file = File.Open(path, FileMode.Open);

                value = serializer.Deserialize(file);
                file.Close();
                return true;
            }
            else
            {
                TextAsset textAsset = (TextAsset)Resources.Load(path);
                if (textAsset != null)
                {
                    value = serializer.Deserialize(textAsset);
                    return true;
                }
            }
            Debug.LogError("The file you are trying to load does not exist : (Path : " + path + " )");
            value = default(Class);
            return false;
        }

        public static bool TryLoad<Class>(string path, out Class value, bool fromResources, SerializerType type = SerializerType.XML)
        {
            return TryLoad<Class, Class>(path, out value, fromResources, type);
        }

        public static bool TryLoad<Class>(string path, out Class value, SerializerType type = SerializerType.XML)
        {
            return TryLoad<Class, Class>(path, out value, false, type);
        }

        public static bool TryLoad<SubClass, Class>(string path, out Class value, SerializerType type = SerializerType.XML) where SubClass : Class
        {
            return TryLoad<SubClass, Class>(path, out value, false, type);
        }

        private static string GetPath<Class>(string path, Serializer<Class> serializer)
        {
            path = path + serializer.Extension;
            return path;
        }
    }

    internal interface Serializer<Class>
    {
        string Extension
        {
            get;
        }

        void Serialize(FileStream file, Class objectToSerialise);

        Class Deserialize(FileStream file);

        Class Deserialize(TextAsset textAsset);
    }

    internal class MyXmlSerializer<Class> : Serializer<Class>
    {

        XmlSerializer serializer = new XmlSerializer(typeof(Class));

        public string Extension
        {
            get
            {
                return ".txt";
            }
        }

        public Class Deserialize(TextAsset textAsset)
        {
            StringReader reader = new StringReader(textAsset.text);
            return (Class)serializer.Deserialize(reader);
        }

        public Class Deserialize(FileStream file)
        {
            return (Class)serializer.Deserialize(file);
        }

        public void Serialize(FileStream file, Class objectToSerialise)
        {
            serializer.Serialize(file, objectToSerialise);
        }
    }

#if !UNITY_WP_8_1
    internal class MyBinaryFormatter<Class> : Serializer<Class>
    {

        BinaryFormatter serializer = new BinaryFormatter();
        public string Extension
        {
            get
            {
                return ".dat";
            }
        }

        public Class Deserialize(TextAsset textAsset)
        {
            MemoryStream stream = new MemoryStream(textAsset.bytes);
            return (Class)serializer.Deserialize(stream);
        }

        public Class Deserialize(FileStream file)
        {
            return (Class)serializer.Deserialize(file);
        }

        public void Serialize(FileStream file, Class objectToSerialise)
        {
            serializer.Serialize(file, objectToSerialise);
        }
    }
#endif
}

