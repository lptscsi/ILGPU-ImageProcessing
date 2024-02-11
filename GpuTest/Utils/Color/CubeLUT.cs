using GpuTest.GPU.Core;
using System;
using System.IO;
using System.Linq;

namespace GpuTest.Utils.Color
{
    /// <summary>
    /// 3D LUT
    /// </summary>
    public class CubeLUT: IDisposable
    {
        private static readonly IFormatProvider formatProvider = System.Globalization.CultureInfo.InvariantCulture;
        const string SIZE_TOKEN = "LUT_3D_SIZE";
        const string RANGE_TOKEN = "LUT_3D_INPUT_RANGE";
        const string DOMAIN_MIN_TOKEN = "DOMAIN_MIN";
        const string DOMAIN_MAX_TOKEN = "DOMAIN_MAX";

        const int N_CHANNELS = 3;
        private GPUBuffer4D<float> _lutBuffer;

        // the size of the LUT typically 17, 33, 65
        public int LutSize { get; private set; }

        // the length equals to the number of channels
        private float[] DomainMin { get; set; }
        //the length equals to the number of channels
        private float[] DomainMax { get; set; }

        public GPUBuffer4D<float> LutBuffer { get => _lutBuffer; }
  
        public void LoadFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException(filePath);
            }

            int lineNum = -1;
            LutSize = 0;

            using (StreamReader reader = new StreamReader(filePath))
            {
                lineNum = ParseLUTParameters(reader);

                _lutBuffer?.Dispose();
                _lutBuffer = new GPUBuffer4D<float>(LutSize, LutSize, LutSize, N_CHANNELS);
            }

            if (LutSize < 10)
            {
                throw new InvalidOperationException("Invalid 3D LUT size");
            }

            using (StreamReader reader = new StreamReader(filePath))
            {
                ParseLUT(lineNum, reader);
            }

            //var data = _lutBuffer.CpuData;
        }

        private int ParseLUTParameters(StreamReader reader)
        {
            DomainMin = new float[N_CHANNELS];
            DomainMax = new float[N_CHANNELS];

            int cnt = 0;
            bool isOK = true;
            while (isOK)
            {
                string line = reader.ReadLine();
                cnt++;

                if (!string.IsNullOrEmpty(line))
                {
                    line = line.Trim();
                }

                // EOF
                if (line == null)
                {
                    throw new InvalidOperationException("Invalid cube file");
                }
                // EMPTY LINES
                else if (line == string.Empty)
                {
                    continue;
                }
                // COMMENTS
                else if (line.StartsWith('#'))
                {
                    /*
                    ReadOnlySpan<char> lineSpan = line.AsSpan(1);
                    int index = lineSpan.IndexOf(':');
                    if (index != -1)
                    {
                        string key = new string(lineSpan.Slice(0, index));
                        string value = new string(lineSpan.Slice(index + 1));
                        switch (key.ToLower())
                        {
                            case "title":
                                this.Title = value;
                                break;
                            case "gamma":
                                this.Gamma = value;
                                break;
                            case "gamut":
                                this.Gamut = value;
                                break;
                            case "version":
                                this.Version = value;
                                break;
                        }
                    }
                    else
                    {
                        Author = new string(lineSpan);
                    }
                    */
                    continue;
                }
                else if (line.StartsWith(SIZE_TOKEN))
                {
                    string[] parts = line.Split(' ');
                    if (parts.Length != 2)
                    {
                        throw new InvalidOperationException("Invalid header format");
                    }
                    string size = parts[1];
                    if (int.TryParse(size, formatProvider, out int value))
                    {
                        this.LutSize = value;
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid 3D LUT size");
                    }
                }
                else if (line.StartsWith(RANGE_TOKEN))
                {
                    string[] parts = line.Split(' ').Skip(1).ToArray();
                    if (parts.Length != 2)
                    {
                        throw new InvalidOperationException("Invalid header format");
                    }
                    string range_start = parts[0];
                    string range_end = parts[1];
                    if (float.TryParse(range_start, formatProvider, out float value1) &&
                        float.TryParse(range_end, formatProvider, out float value2))
                    {
                        for(int i = 0; i< N_CHANNELS; ++i)
                        {
                            DomainMin[i] = value1;
                            DomainMax[i] = value2;
                        }
                    }
                    else
                    {
                        throw new InvalidOperationException("Invalid 3D LUT range");
                    }
                }
                else if (line.StartsWith(DOMAIN_MIN_TOKEN))
                {
                    string[] parts = line.Split(' ').Skip(1).ToArray();

                    if (parts.Length != N_CHANNELS)
                    {
                        throw new InvalidOperationException("Invalid header format");
                    }

                    for (int i = 0; i < N_CHANNELS; ++i)
                    {
                        if (float.TryParse(parts[i], formatProvider, out float value))
                        {
                            DomainMin[i] = value;
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid 3D LUT DOMAIN_MIN");
                        }
                    }
                }
                else if (line.StartsWith(DOMAIN_MAX_TOKEN))
                {
                    string[] parts = line.Split(' ').Skip(1).ToArray();

                    if (parts.Length != N_CHANNELS)
                    {
                        throw new InvalidOperationException("Invalid header format");
                    }

                    for (int i = 0; i < N_CHANNELS; ++i)
                    {
                        if (float.TryParse(parts[i], formatProvider, out float value))
                        {
                            DomainMax[i] = value;
                        }
                        else
                        {
                            throw new InvalidOperationException("Invalid 3D LUT DOMAIN_MAX");
                        }
                    }
                }
                else if (char.IsDigit(line[0]))
                {
                    // line at which the table starts 
                    return cnt;
                }
            }

            throw new InvalidOperationException("Invalid cube file");
        }

        private void ParseLUT(int lineNum, StreamReader reader)
        {
            int cnt = 1;
         
            while (cnt < lineNum)
            {
                _ = reader.ReadLine();
                cnt++;
            }

         
            int N = this.LutSize;

            for (int b = 0; b < N; ++b)
            {
                for (int g = 0; g < N; ++g)
                {
                    for (int r = 0; r < N; ++r)
                    {
                        string line = reader.ReadLine();
                        ParseTableRow3D(line, r, g, b);
                    }
                }
            }
        }

        private void ParseTableRow3D(string line, int r, int g, int b)
        {
            string[] parts = line.Split(' ');

            if (parts.Length < 3)
            {
                throw new InvalidOperationException("Expected to have parts for 3 channels");
            }
     
            for (int i = 0; i < N_CHANNELS; ++i)
            {
                float value = float.Parse(parts[i], formatProvider);
                _lutBuffer.Set(value, r, g, b, i);
            }
        }

        public void Dispose()
        {
           _lutBuffer?.Dispose();
        }
    }
}
