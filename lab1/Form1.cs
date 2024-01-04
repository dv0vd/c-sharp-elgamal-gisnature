using System;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.Numerics;
using System.Collections.Generic;

namespace lab1
{
    public partial class Form1 : Form
    {
        private static ulong[] simple_numbers = {2,3,5,7,11,13,17,19,23,29,31,37,41,43,47,
            53,59,61,67,71,73,79,83,89,97,101,103,107,109,113,127,131,137,139,149,
            151,157,163,167,173,179,181,191,193,197,199,211,223,227,229,233,239,
            241,251,257,263,269,271,277,281,283,293,307,311,313,317,331,337,347,
            349,353,359,367,373,379,383,389,397,401,409,419,421,431,433,439,443,449,457,461,463,467,479,487,491,499 };
        private const string inputSigniture = "Файл ЭЦП", inputFile = "Входной файл...", outputFile = "Каталог сохранения выходных файлов...", keysFile = "Файл с ключами...";
        private string inputFileName, outputDirectory, inputSignitureFile, inputKeysFile;
        private static uint[] H = { 0x6A09E667, 0xBB67AE85, 0x3C6EF372, 0xA54FF53A, 0x510E527F, 0x9B05688C, 0x1F83D9AB, 0x5BE0CD19 };
        private static uint[] K = { 0x428A2F98, 0x71374491, 0xB5C0FBCF, 0xE9B5DBA5, 0x3956C25B, 0x59F111F1, 0x923F82A4, 0xAB1C5ED5,
        0xD807AA98, 0x12835B01, 0x243185BE, 0x550C7DC3, 0x72BE5D74, 0x80DEB1FE, 0x9BDC06A7, 0xC19BF174,
        0xE49B69C1, 0xEFBE4786, 0x0FC19DC6, 0x240CA1CC, 0x2DE92C6F, 0x4A7484AA, 0x5CB0A9DC, 0x76F988DA,
        0x983E5152, 0xA831C66D, 0xB00327C8, 0xBF597FC7, 0xC6E00BF3, 0xD5A79147, 0x06CA6351, 0x14292967,
        0x27B70A85, 0x2E1B2138, 0x4D2C6DFC, 0x53380D13, 0x650A7354, 0x766A0ABB, 0x81C2C92E, 0x92722C85,
        0xA2BFE8A1, 0xA81A664B, 0xC24B8B70, 0xC76C51A3, 0xD192E819, 0xD6990624, 0xF40E3585, 0x106AA070,
        0x19A4C116, 0x1E376C08, 0x2748774C, 0x34B0BCB5, 0x391C0CB3, 0x4ED8AA4A, 0x5B9CCA4F, 0x682E6FF3,
        0x748F82EE, 0x78A5636F, 0x84C87814, 0x8CC70208, 0x90BEFFFA, 0xA4506CEB, 0xBEF9A3F7, 0xC67178F2 };

        public Form1()
        {
            InitializeComponent();
            label4.Text = inputSigniture;
        }

        long gcdExtended(long a, long b, out long x, out long y)
        {
            if (a == 0)
            {
                x = 0;
                y = 1;
                return b;
            }

            long x1, y1; // Для сохранения результатов рекурсивного вызова
            long gcd = gcdExtended(b % a, a, out x1, out y1);

            x = y1 - (b / a) * x1;
            y = x1;
            return gcd;
        }

        // Алгоритм быстрого возведения в степень
        private static long Power(BigInteger x, BigInteger y, long mod)
        {
            BigInteger count = 1;
            if (y == 0) return 1;
            while (y > 0)
            {
                if (y % 2 == 0)
                {
                    y /= 2;
                    x *= x;
                    x %= mod;
                }
                else
                {
                    y--;
                    count *= x;
                    count %= mod;
                }
            }
            return (long)(count % mod);
        }

        // Вычисление первообразного корня (1ое задание) (само вычисление корней)
        private long FindPrimitiveRoot(long num)
        {
            // Факторизация fi(num)
            List<long> factorization = new List<long>();
            long b, c;
            long fiMasCount = num - 1; // т.к. num 100% простое, что функция эйлера на 1 меньше num
            long temp = fiMasCount;

            while ((temp % 2) == 0)
            {
                temp = temp / 2;
                if (!factorization.Contains(2))
                    factorization.Add(2);
            }
            b = 3;
            c = (long)Math.Sqrt(temp) + 1;
            while (b < c)
            {
                if ((temp % b) == 0)
                {
                    if (temp / b * b - temp == 0)
                    {
                        if (!factorization.Contains(b))
                            factorization.Add(b);
                        temp = temp / b;
                        c = (long)Math.Sqrt(temp) + 1;
                    }
                    else
                        b += 2;
                }
                else
                    b += 2;
            }
            if (!factorization.Contains(temp))
                factorization.Add(temp);
            // Проверка каждоого основания [2...num-1]
            for (long i = 2; i < num; i++)
            {
                bool check = true;
                foreach (long a in factorization)
                {
                    if (Power(i, fiMasCount / a, num) % num == 1)
                    {
                        check = false;
                        break;
                    }
                }
                if (check)
                    return i;
            }
            return 0;
        }

        // Тест Рабина-Миллера для полученного простого числа
        private bool RMGenerate(BigInteger n)
        {
            int s = 0;
            BigInteger r = n - 1;

            while (r % 2 == 0)
            {
                s += 1;
                r /= 2;
            }
            for (int i = 0; i < 5; i++)
            {
                BigInteger a;
                byte[] _a = new byte[n.ToByteArray().LongLength];
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                do
                {
                    rng.GetBytes(_a);
                    a = new BigInteger(_a);
                }
                while (a < 2 || a >= n - 2);
                BigInteger y = BigInteger.ModPow(a, r, n);
                if ((y == 1) || (y == n - 1))
                {
                    continue;
                }
                for (int j = 1; j < s; j++)
                {
                    y = (y * y) % n;
                    if (y == 1)
                    {
                        return false;
                    }
                    if (y == (n - 1))
                        break;
                }
                if (y != (n - 1))
                {
                    return false;
                }
            }
            return true;
        }

        // Генерация простого большого числа
        private long GenerateBigSimple()
        {
            while (true)
            {
                bool check = false;
                long number;
                while (true)
                {
                    var r = new Random(Guid.NewGuid().GetHashCode());
                    var b = new byte[sizeof(int)];
                    r.NextBytes(b);
                    var num = BitConverter.ToInt32(b, 0);
                    if (num > 0)
                    {
                        number = num;
                        break;
                    }
                }
               

                number |= 1; // для того, чтобы число не было чётным
                // Проверяем делится ли оно на первые 500 простых чисел
                for (int i = 0; i < simple_numbers.Length; i++)
                {
                    if (number % (long)simple_numbers[i] == 0) // если да - полученное число не простое, повтор
                    {
                        check = true;
                        break;
                    }
                }
                // если успешно пройдена преыдущая проверка, то проводим тест рабина миллера 5 араз
                if (!check)
                {
                    if (RMGenerate(number))
                    {
                        return number;
                    }
                }
            }
        }

        // Выбор входного файла для подписи
        private void button1_Click(object sender, EventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "Файлы |*";
            OPF.Title = "Выбрать файл";
            if (OPF.ShowDialog() == DialogResult.OK)
            {
                inputFileName = OPF.FileName;
                label1.Text = inputFileName;
            }
        }

        private void Finish()
        {
            label1.Text = inputFile;
            label2.Text = outputFile;
            label4.Text = inputSigniture;
            label3.Text = keysFile;
        }

        //обычный алгоритм Евклида через остатки
        long Nod(long a, long b)
        {
            while ((a > 0) && (b > 0))
                if (a >= b)
                    a %= b;
                else
                    b %= a;
            return a | b;
        }

        // Начало процесса подписи
        private void button3_Click(object sender, EventArgs e)
        {
            if ((label1.Text == inputFile) || (label2.Text == outputFile))  //проверка выбора файлов
            {
                MessageBox.Show("Не выбраны пути к файлам!");
            }
            else
            {
                long p = GenerateBigSimple();
                long g = FindPrimitiveRoot(p);
                int x = GetRanodom(true);
                long y = Power(g, x, p);
                string path = outputDirectory + "\\ЭЦП_ключи.txt";
                StreamWriter writer = new StreamWriter(path);
                writer.WriteLine(y);
                writer.WriteLine(p);
                writer.WriteLine(g);
                writer.Close();
                FileStream fstream = new FileStream(inputFileName, FileMode.Open);
                byte[] file = new byte[fstream.Length];
                fstream.Read(file, 0, file.Length);
                path = outputDirectory + "\\ЭЦП";
                FileStream foutstream = new FileStream(path, FileMode.Create);
                byte[] outData;
                byte[] dataSize = new byte[1];
                for (int i = 0; i < file.Length; i++)
                {
                    int k;
                    while (true)
                    {
                        k = GetRanodom(false);
                        if (Nod((long)k, (p - 1)) == 1)
                            break;
                    }
                    long a = (long)BigInteger.ModPow(g, k, p);
                    long xx, yy;
                    gcdExtended(k, (p - 1), out xx, out yy);
                    long k1 = (xx % (p-1) + (p-1)) % (p-1);
                    long temp = (k1 * (file[i] - x * a));
                    long b = ( temp % (p - 1));
                    if(b<0)
                    {
                        b = (p - 1 + b);
                    }
                    outData = BitConverter.GetBytes(a);
                    dataSize[0] = (byte)outData.Length;
                    foutstream.Write(dataSize, 0, dataSize.Length);
                    foutstream.Write(outData, 0, outData.Length);
                    outData = BitConverter.GetBytes(b);
                    dataSize[0] = (byte)outData.Length;
                    foutstream.Write(dataSize, 0, dataSize.Length);
                    foutstream.Write(outData, 0, outData.Length);
                }
                fstream.Close();
                foutstream.Close();
                MessageBox.Show("Успех!");
                Finish();
            }
        }

        // Выбор файла с ключами
        private void button6_Click(object sender, EventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "Файлы |*";
            OPF.Title = "Выбрать файл";
            if (OPF.ShowDialog() == DialogResult.OK)
            {
                inputKeysFile = OPF.FileName;
                label3.Text = inputKeysFile;
            }
        }

        // Выбор директории сохранения ЭЦП
        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog FBD = new FolderBrowserDialog();
            if (FBD.ShowDialog() == DialogResult.OK)
            {
                outputDirectory = FBD.SelectedPath;
                FBD.Description = "Выбрать директорию";
                label2.Text = outputDirectory;
            }
        }

        // Выбор файла ЭЦП
        private void button5_Click(object sender, EventArgs e)
        {
            OpenFileDialog OPF = new OpenFileDialog();
            OPF.Filter = "Файлы |*";
            OPF.Title = "Выбрать файл";
            if (OPF.ShowDialog() == DialogResult.OK)
            {
                inputSignitureFile = OPF.FileName;
                label4.Text = inputSignitureFile;
            }
        }

        // Начало проверки подписи
        private void button4_Click(object sender, EventArgs e)
        {
            if ((label1.Text == inputFile) || (label4.Text == inputSigniture) || (label3.Text == keysFile))  //проверка выбора файлов
            {
                MessageBox.Show("Не выбраны пути к файлам!");
            }
            else
            {
                StreamReader reader = new StreamReader(inputKeysFile);
                long y = Convert.ToInt64(reader.ReadLine());
                long p = Convert.ToInt64(reader.ReadLine());
                long g = Convert.ToInt64(reader.ReadLine());
                reader.Close();
                if ((y == 0) || (p == 0) || (g == 0))
                {
                    MessageBox.Show("Цифровая подпись не совпадает!");
                    Finish();
                    return;
                }
                FileStream fstream = new FileStream(inputSignitureFile, FileMode.Open);
                FileStream fstreamBasicFile = new FileStream(inputFileName, FileMode.Open);
                byte[] file = new byte[fstream.Length];
                byte[] basicFile = new byte[fstreamBasicFile.Length];
                fstream.Read(file, 0, file.Length);
                fstreamBasicFile.Read(basicFile, 0, basicFile.Length);
                if(file.Length != (basicFile.Length*18))
                {
                    MessageBox.Show("Цифровая подпись не совпадает!");
                    fstream.Close();
                    fstreamBasicFile.Close();
                    Finish();
                    return;
                }
                bool error = false;
                int offset = 0;
                for(int i=0; i< fstreamBasicFile.Length; i++)
                {
                    if (offset >= file.Length)
                    {
                        MessageBox.Show("Цифровая подпись не совпадает!");
                        fstream.Close();
                        fstreamBasicFile.Close();
                        Finish();
                        return;
                    }
                    byte[] size = new byte[1];
                    size[0] = file[offset];
                    byte[] data = new byte[size[0]];
                    for (byte j = 0; j < size[0]; j++)
                    {
                        data[j] = file[j+offset+1];
                    }
                    offset += size[0] + 1;
                    long a = BitConverter.ToInt64(data, 0);
                    size[0] = file[offset];
                    data = new byte[size[0]];
                    for (byte j = 0; j < size[0]; j++)
                    {
                        data[j] = file[j + offset + 1];
                    }
                    offset += size[0] + 1;
                    long b = BitConverter.ToInt64(data, 0);
                    BigInteger t1 = BigInteger.ModPow(y,a,p);
                    BigInteger t2 = BigInteger.ModPow(a, b, p);
                    BigInteger c1 = (t1 * t2) % p;
                    BigInteger c2 = BigInteger.ModPow(g, basicFile[i], p);
                    if(c1 != c2)
                    {
                        error = true;
                        break;
                    }
                }
                if (!error)
                {
                    if (offset < file.Length)
                    {
                        MessageBox.Show("Цифровая подпись не совпадает!");
                    }
                    else
                    {
                        MessageBox.Show("Цифровая подпись совпадает!");
                    }
                }
                else
                {
                    MessageBox.Show("Цифровая подпись не совпадает!");
                }
                fstream.Close();
                fstreamBasicFile.Close();
                Finish();
            }
        }

        // Получение рандомного числа
        private int GetRanodom(bool x)
        {
            Random rnd = new Random(Guid.NewGuid().GetHashCode());
            int value;
            if (x)
            {
                 value = rnd.Next(100) + 2;
            }
            else
            {
                 value = rnd.Next(9) + 2;
            }
            value |= 1;
            return value;
        }
    }
}
