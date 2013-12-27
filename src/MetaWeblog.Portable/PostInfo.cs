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
    public class PostInfo
    {

        #region Properties

        /// <summary>
        /// 
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Link { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public DateTime? DateCreated { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PostId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public int CommentCount { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string PostStatus { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Permalink { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string Description { get; set; }

        #endregion

        #region Public Methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task<List<PostInfo>> GetDrafts()
        {
            var drafts = new List<PostInfo>();
            var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Drafts");
            var files = await folder.GetFilesAsync();

            foreach (var file in files)
            {
                var contents = await file.ReadAllTextAsync();
                var connection = JsonConvert.DeserializeObject<PostInfo>(contents);
                drafts.Add(connection);
            }
            return drafts;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public static async Task<BlogConnectionInfo> GetDraft(string title)
        {
            var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Drafts");
            var file = await folder.GetFileAsync(title + ".json");
            var contents = await file.ReadAllTextAsync();
            var connection = JsonConvert.DeserializeObject<BlogConnectionInfo>(contents);
            return connection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<bool> SaveDraft(string filename)
        {
            try
            {
                var folder = await FileSystem.Current.LocalStorage.GetFolderAsync("Drafts");
                var file = await folder.CreateFileAsync(Title + ".json", CreationCollisionOption.OpenIfExists);
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

        #endregion

    }
}