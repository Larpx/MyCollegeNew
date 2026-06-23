using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Web;

/// <summary>
/// 文件操作类
/// 指定文件的写操作
/// 指定文件的读取操作
/// csv文件导出
/// </summary>
public static class FileHelper
{

    /// <summary>
    /// 将文件名和文件路径拼接起来
    /// </summary>
    /// <param name="path">路径名</param>
    /// <param name="name">文件名</param>
    /// <returns>拼接好的路径名，如将D:\path\etc和i.txt拼接的结果为D:\\path\\etc\\i.txt</returns>
    public static string getfile(string path, string name)
    {
        if (String.IsNullOrEmpty(path) && String.IsNullOrEmpty(name))
        {

            return null;
        }
        else
        {
            string fullname = null;
            path.Trim();
            name.Trim();
            if (path.EndsWith("\\"))
            {
                //路径字符串拼接，其实两者效果一样，就优先选用系统提供的函数
                // fullname = String.Concat(path,  name); 
                fullname = Path.Combine(path, name);
            }
            else
            {
                //fullname = String.Concat(path, "\\", name);
                fullname = Path.Combine(path, name);

            }
            return fullname;
        }
    }


    /// <summary>
    /// 提供文件读取操作，返回Dictionary泛型集合
    /// </summary>
    /// <param name="filepath">文件路径</param>
    /// <returns>Dictionary泛型集合</returns>
    public static Dictionary<string, string> ReadFile(string filepath)
    {
        Dictionary<string, string> dic = new Dictionary<string, string>();
        string tmp = null;
        if (!String.IsNullOrEmpty(filepath))
        {
            using (StreamReader sr = new StreamReader(filepath, Encoding.Default))
            {
                //开始按照行读入文件流
                while ((tmp = sr.ReadLine()) != null)
                {
                    //前一行为参数，后一行为参数的值
                    dic.Add(tmp, sr.ReadLine());
                }
            }
            return dic;
        }
        else
        {

            return null;
        }
    }


    /// <summary>
    /// 文件读取操作，将结果保存在字符串中
    /// </summary>
    /// <param name="filepath">文件所在路径，绝对或者相对</param>
    /// <returns>读取到的内容</returns>
    public static string ReadFile(string filepath, int i)
    {

        string ret = null;
        if (!String.IsNullOrEmpty(filepath))
        {
            using (StreamReader fs = new StreamReader(filepath, Encoding.Default))
            {
                ret = fs.ReadToEnd();
            }
            return ret;
        }
        else
        {
            return null;
        }
    }

    /// <summary>
    /// 文件写入操作
    /// </summary>
    /// <param name="input">要写入的信息</param>
    /// <param name="filepath">文件路径</param>
    /// <returns>-1失败，1成功</returns>
    public static int WriteFile(string input, string filepath)
    {
        if (String.IsNullOrEmpty(filepath) && String.IsNullOrEmpty(input))
        {
            return -1;
        }
        else
        {
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate))
            {
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.Write(input);
                    return 1;
                }
            }
        }
    }

    /// <summary>
    /// 将要写入的字符串转换为字节流在进行写入
    /// </summary>
    /// <param name="input">要写入的写入的字符串</param>
    /// <param name="filepath">文件路径</param>
    /// <param name="index">无用标志位</param>
    /// <returns>-1失败，1成功</returns>
    public static int WriteFile(string input, string filepath, int index)
    {

        if (String.IsNullOrEmpty(filepath) && String.IsNullOrEmpty(input))
        {
            return -1;
        }
        else
        {
            using (FileStream fs = new FileStream(filepath, FileMode.OpenOrCreate))
            {
                //将要写入的信息转换为字节流
                byte[] buf = Encoding.Default.GetBytes(input);
                fs.Write(buf, 0, buf.Length);
                return 1;
            }
        }

    }



    /// <summary>
    /// 将指定信息输出为CSV格式的文件
    /// </summary>
    /// <param name="str">需要输出的数据</param>
    /// <param name="path">路径</param>
    /// <returns>-1失败，1成功</returns>
    public static int OutCsv(string str, string path)
    {
        int index = 0;
        index = FileHelper.WriteFile(str, path);
        if (index != 1)
        {
            return -1;
        }
        else
        {
            return 1;
        }
    }
}
