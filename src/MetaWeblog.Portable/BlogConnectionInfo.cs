using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PCLStorage;

namespace MetaWeblog.Portable
{


    /// <summary>
    /// 
    /// </summary>
    public class BlogConnectionInfo
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string BlogUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string MetaWeblogUrl { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string BlogId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Password { get; set; }

        #endregion

        #region Constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="blogUrl"></param>
        /// <param name="metaWeblogUrl"></param>
        /// <param name="blogId"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        public BlogConnectionInfo(string blogUrl, string metaWeblogUrl, string blogId, string username, string password)
        {
            BlogUrl = blogUrl;
            BlogId = blogId;
            MetaWeblogUrl = metaWeblogUrl;
            Username = username;
            Password = password;
        }

        #endregion

        /// <summary>
        /// Retrieves a list of all of the connections that have been saved to the LocalStorage cache.
        /// </summary>
        /// <returns>A List of <see cref="BlogConnectionInfo"/> objects saved to LocalStorage.</returns>
        public static async Task<List<BlogConnectionInfo>> GetConnections()
        {
            var connections = new List<BlogConnectionInfo>();
            var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Connections");
            var files = await folder.GetFilesAsync();

            foreach (var file in files)
            {
                var contents = await file.ReadAllTextAsync();
                var connection = JsonConvert.DeserializeObject<BlogConnectionInfo>(contents);
                connections.Add(connection);
            }
            return connections;
        }

        /// <summary>
        /// Retrieves a specific connection from the LocalStorage cache.
        /// </summary>
        /// <returns>A <see cref="BlogConnectionInfo"/> that was saved to LocalStorage.</returns>
        public static async Task<BlogConnectionInfo> GetConnection(string blogUrl)
        {
            var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Connections");
            var uri = new Uri(blogUrl);
            var file = await folder.GetFileAsync(uri.Host + ".json");
            var contents = await file.ReadAllTextAsync();
            var connection = JsonConvert.DeserializeObject<BlogConnectionInfo>(contents);
            return connection;
        }

        /// <summary>
        /// Saves this Connection to the LocalStorage cache.
        /// </summary>
        /// <returns>A boolean specifying whether or not the file was saved.</returns>
        public async Task<bool> Save()
        {
            try
            {
                var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Connections");
                var uri = new Uri(BlogUrl);
                var file = await folder.CreateFileAsync(uri.Host + ".json", CreationCollisionOption.OpenIfExists);
                var contents = JsonConvert.SerializeObject(this, Formatting.Indented);
                await file.WriteAllTextAsync(contents);
                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                return false;
            }
        }
        


    }
}