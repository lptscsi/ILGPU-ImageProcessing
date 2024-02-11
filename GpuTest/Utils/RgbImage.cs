using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace GpuTest
{
    /// <summary>
    /// Класс для сохранения RGB байт в файле
    /// </summary>
    public sealed class RgbImage
    {
        private const int BUFFER_SIZE = 1024 * 64;

        /// <summary>
        /// Структура для хранения информации в заголовке
        /// </summary>
        public struct Info
        {
            public int Channels { get; init; }
            public ColorDepths ColorDepth { get; init; }
            public int Width { get; init; }
            public int Height { get; init; }
            public int Size { get; init; }
        }

        #region Конструкторы

        public RgbImage()
        {

        }

        public RgbImage(int width, int height, ColorDepths colorDepth, byte[] bytes, int channels = 3)
        {
            Channels = channels;
            Width = width;
            Height = height;
            ColorDepth = colorDepth;
            Bytes = bytes;
        }

        #endregion

        /// <summary>
        /// Кол-во каналов
        /// </summary>
        public int Channels { get; init; }

        /// <summary>
        /// Глубина цвета изображения
        /// </summary>
        public ColorDepths ColorDepth { get; init; }

        /// <summary>
        /// Ширина изображения
        /// </summary>
        public int Width { get; init; }

        /// <summary>
        /// Высота изображения
        /// </summary>
        public int Height { get; init; }

        /// <summary>
        /// Данные
        /// </summary>
        public byte[] Bytes { get; init; }

        #region Методы

        /// <summary>
        /// Сохранить данные в поток
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public async Task Save(Stream stream, CancellationToken cancellationToken = default)
        {
            int size = Bytes?.Length ?? 0;

            // сохранить заголовок
            using (MemoryStream memoryStream = new MemoryStream())
            {
                Info info = new Info() { Channels = Channels, Width = Width, Height = Height, ColorDepth = ColorDepth, Size = size };
                Serializer.SerializeToStream(info, memoryStream, typeof(Info));
                byte[] header = memoryStream.ToArray();

                using (BinaryWriter binaryWriter = new BinaryWriter(stream, Encoding.UTF8, true))
                {
                    binaryWriter.Write(header.Length);
                    binaryWriter.Write(header);
                }
            }

            // сохранить данные
            if (size > 0)
            {
                await stream.WriteAsync(Bytes, cancellationToken);
            }
        }


        /// <summary>
        /// Сохранить данные в файл
        /// </summary>
        /// <returns></returns>
        public async Task Save(string filePath, CancellationToken cancellationToken = default)
        {
            using (FileStream fileStream = File.Create(filePath, BUFFER_SIZE, FileOptions.Asynchronous))
            {
                await Save(fileStream, cancellationToken);
            }
        }

        /// <summary>
        /// Восстановить экземпляр класса из потока
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static async Task<RgbImage> Read(Stream stream, CancellationToken cancellationToken = default)
        {
            Info header;
            byte[] bytes = null;

            using (MemoryStream headerStream = new MemoryStream())
            using (BinaryReader binaryReader = new BinaryReader(stream, Encoding.UTF8, true))
            {
                int len = binaryReader.ReadInt32();
                headerStream.Write(binaryReader.ReadBytes(len));
                headerStream.Position = 0;
                header = Serializer.DeSerializeFromStream<Info>(headerStream);
            }

            if (header.Size > 0)
            {
                using MemoryStream bytesStream = new MemoryStream(header.Size);
                await stream.CopyToAsync(bytesStream, BUFFER_SIZE, cancellationToken);
                bytesStream.Position = 0;
                bytes = bytesStream.GetBuffer();
            }

            return new RgbImage(header.Width, header.Height, header.ColorDepth, bytes, header.Channels);
        }

        /// <summary>
        /// Восстановить экземпляр класса из файла
        /// </summary>
        /// <returns></returns>
        public static async Task<RgbImage> Read(string filePath, CancellationToken cancellationToken = default)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.None, BUFFER_SIZE))
            {
                return await RgbImage.Read(fileStream, cancellationToken);
            }
        }

        #endregion
    }
}
