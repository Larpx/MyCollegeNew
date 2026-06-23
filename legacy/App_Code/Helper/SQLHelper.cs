using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;

/// <summary>
/// SQLHelper 的摘要说明
/// </summary>
public static class SQLHelper
{
    //全局连接对象

    //只读的连接字符串
    public static readonly string connstr = ConfigurationManager.AppSettings["connstr"].ToString();


    /// <summary>
    /// 获取受影响行数，判断SQL语句是否执行成功
    /// 执行成功后，conn对象自动销毁
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <param name="parameters">语句中的变量值</param>
    /// <returns>返回受影响行数</returns>
    public static int ExecuteNonQuery(string sql,
        params SqlParameter[] parameters)
    {
        using (SqlConnection conn = new SqlConnection(connstr))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteNonQuery();
            }
        }
    }

    /// <summary>
    /// 获取制定SQL命令得到的数据集
    /// 非连接式
    /// </summary>
    /// <param name="sql">SQL命令</param>
    /// <param name="parameters">变长，对应SQL的参数</param>
    /// <returns>DataTable</returns>
    public static DataTable ExecuteDataTable(string sql,
        params SqlParameter[] parameters)
    {
        using (SqlConnection conn = new SqlConnection(connstr))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                //执行指定SQL命令
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                DataSet dataset = new DataSet();
                SqlDataAdapter adapter = new SqlDataAdapter(cmd);
                adapter.Fill(dataset);
                return dataset.Tables[0];
            }
        }
    }

    /// <summary>
    /// 连接式数据库操作，速度快
    /// 返回DataReader对象
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <param name="parameters">语句中的变量值</param>
    /// <returns></returns>
    public static SqlDataReader ExecuteDataReader(string sql,
        params SqlParameter[] parameters)
    {
        using (SqlConnection conn = new SqlConnection(connstr))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                //执行指定SQL命令
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);

                //建立DataReader对象
                SqlDataReader datareader = cmd.ExecuteReader();
                return datareader;
            }
        }
    }



    /// <summary>
    /// 获取查询结果的第一行第一列数据
    /// </summary>
    /// <param name="sql">sql语句</param>
    /// <param name="parameters">语句中的变量值</param>
    /// <returns>object类型的数据库中指定的第一行第一列数据</returns>
    public static object ExecuteScalar(string sql,
       params SqlParameter[] parameters)
    {
        using (SqlConnection conn = new SqlConnection(connstr))
        {
            conn.Open();
            using (SqlCommand cmd = conn.CreateCommand())
            {
                cmd.CommandText = sql;
                cmd.Parameters.AddRange(parameters);
                return cmd.ExecuteScalar();
            }
        }
    }

    /// <summary>
    /// 检测传来的值是否是空值，防止发生NullReferenceException 
    /// </summary>
    /// <param name="value">需要进行判断的值</param>
    /// <returns>返回判断的结果</returns>
    public static object FromDbValue(object value)
    {
        if (value == DBNull.Value)
        {
            return null;
        }
        else
        {
            return value;
        }
    }

    /// <summary>
    /// 判断传递的值，防止发生NullReferenceException 
    /// </summary>
    /// <param name="value">需要进行判断的值</param>
    /// <returns>返回判断的结果</returns>
    public static object ToDbValue(object value)
    {
        if (value == null)
        {
            return DBNull.Value;
        }
        else
        {
            return value;
        }
    }
}