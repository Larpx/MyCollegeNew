using System;
using System.Text;
using System.Security.Cryptography;
using System.Web;

/// <summary>
/// 提供MD5操作
/// 1.提供指定字符串的MD5加密操作
/// 2.提供两个MD5值的对比操作
/// </summary>
public static class Md5Helper
{

    /// <summary>
    /// 获取指定字符串的MD5加密字符串
    /// </summary>
    /// <param name="tmp">需要换的值</param>
    /// <returns>16进制的MD5字符串</returns>
    public static string GetMd5(string tmp)
    {
        string ret = null;
        MD5 md5 = MD5.Create();
        byte[] strbuf = Encoding.Default.GetBytes(tmp);
        byte[] md5buf = md5.ComputeHash(strbuf);
        //注意编码问题           
        for (int i = 0; i < md5buf.Length; i++)
        {
            ret += md5buf[i].ToString("x2");//转换为16进制
        }
        return ret;
    }

    /// <summary>
    /// 对比输入的两个MD5字符串
    /// </summary>
    /// <param name="tmp1">第一个16进制的MD5字符串</param>
    /// <param name="tmp2">第二个16进制的MD5字符串</param>
    /// <returns>相同为true，不同为false</returns>
    public static bool JudjeMd5(string tmp1, string tmp2)
    {
        bool isWhat = String.Equals(tmp1, tmp2, StringComparison.Ordinal);
        return isWhat;

    }
}
