using System;

namespace MetaWeblog.Portable.Server
{
    public class StringUtils
    {
        public static string CollapseWhiteSpace(string input_string)
        {
            string s = input_string.Trim();
            bool iswhite = false;
            var sb = new System.Text.StringBuilder(s.Length);
            foreach (char c in s)
            {
                if (Char.IsWhiteSpace(c))
                {
                    if (!iswhite)
                    {
                        sb.Append(" ");
                        iswhite = true;
                    }
                }
                else
                {
                    sb.Append(c);
                    iswhite = false;
                }
            }
            return sb.ToString();
        }

    }
}