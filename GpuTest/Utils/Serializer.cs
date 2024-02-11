using Newtonsoft.Json;
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace GpuTest
{
    /// <summary>
    /// Вспомогательный класс для сериализации и десериализации объектов
    /// </summary>
    public static class Serializer
    {
        /// <summary>
        /// Сериализует обьект в файл
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="path"></param>
        /// <param name="type"></param>
        public static void SerializeToFile(this object instance, string path, Type type)
        {
            using FileStream fs = new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, 4096, FileOptions.SequentialScan);

            Serializer.SerializeToStream(instance, fs, type);
        }
        /// <summary>
        /// Сериализует обьект в файл
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="path"></param>
        public static void SerializeToFile<T>(this T instance, string path)
        {
            SerializeToFile(instance, path, typeof(T));
        }

        /// <summary>
        /// Сериализует обьект в поток
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="stream"></param>
        /// <param name="type"></param>
        public static void SerializeToStream(this object instance, Stream stream, Type type)
        {
            using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8, -1, leaveOpen: true);

            writer.Write(JsonConvert.SerializeObject(instance, type, Formatting.Indented, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            }));
        }


        /// <summary>
        /// Сериализует обьект в поток
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="stream"></param>
        /// <param name="type"></param>
        public static void SerializeToStream<T>(this T instance, Stream stream)
        {
            SerializeToStream(instance, stream, typeof(T));
        }

        /// <summary>
        ///  Сериализует обьект в строку
        /// </summary>
        /// <param name="instance"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static string SerializeToString(this object instance, Type type)
        {
            JsonSerializerSettings serSettings = new JsonSerializerSettings() { TypeNameHandling = TypeNameHandling.Auto };
            return JsonConvert.SerializeObject(instance, type, Formatting.Indented, serSettings);
        }

        /// <summary>
        ///  Сериализует обьект в строку
        /// </summary>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static string SerializeToString<T>(this T instance)
        {
            return SerializeToString(instance, typeof(T));
        }


        /// <summary>
        /// Десериализует объект из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="path"></param>
        /// <returns></returns>
        public static T DeSerializeFromFile<T>(string path)
        {
            using FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.SequentialScan);

            return Serializer.DeSerializeFromStream<T>(fs);
        }

        /// <summary>
        /// Десериализует объект из потока
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static T DeSerializeFromStream<T>(Stream stream)
        {
            using StreamReader reader = new StreamReader(stream, Encoding.UTF8, false, -1, leaveOpen: true);

            T result = JsonConvert.DeserializeObject<T>(reader.ReadToEnd(), new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.Auto,
            });

            return result;
        }

        /// <summary>
        /// Десериализует объект из строкового содержимого
        /// </summary>
        public static T DeSerializeFromString<T>(this string content)
        {
            T result = JsonConvert.DeserializeObject<T>(content, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto });
            return result;
        }

        /// <summary>
        /// Исправить сериализацию объекта
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="tempInstance"></param>
        /// <param name="content"></param>
        /// <returns></returns>
        public static T RepairSerialization<T>(T tempInstance, string content)
        {
            string serializedProject = tempInstance.SerializeToString(typeof(T));

            string[] fileContentLines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            string str = $@"
                    {fileContentLines.First()}
                    {serializedProject.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).ElementAt(1)}
                    {string.Join(Environment.NewLine, fileContentLines.Skip(1))}";

            return str.DeSerializeFromString<T>();
        }

        /// <summary>
        /// Создает клон объекта
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="instance"></param>
        /// <returns></returns>
        public static T Clone<T>(T instance, Type type)
        {
            return (T)JsonConvert.DeserializeObject
            (
                JsonConvert.SerializeObject(instance, new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }),
                type,
                new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.Auto }
            );
        }
    }
}
