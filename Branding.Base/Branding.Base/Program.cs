using System.Linq;
using Microsoft.SharePoint.Client;
using System;
using System.IO;
using System.Security;

namespace Branding.Base
{
    class Program
    {
        static void Main(string[] args)
        {
            // Request Office365 site from the user
            string siteUrl = GetSite();

            /* Prompt for Credentials */
            Console.WriteLine("Enter Credentials for {0}", siteUrl);

            string userName = GetUserName();
            SecureString pwd = GetPassword();

            /* End Program if no Credentials */
            if (string.IsNullOrEmpty(userName) || (pwd == null))
                return;

            // Get access to source site
            using (var ctx = new ClientContext(siteUrl))
            {
                ctx.AuthenticationMode = ClientAuthenticationMode.Default;
                //Uncomment for ONPrem 
                //ctx.Credentials = new System.Net.NetworkCredential(userName, pwd);
                //Uncomment for Online
                ctx.Credentials = new SharePointOnlineCredentials(userName, pwd);

                Web web = ctx.Web;
                var allProperties = web.AllProperties;

                ctx.Load(web);
                ctx.Load(allProperties);
                ctx.ExecuteQuery();

                ProcessDirectory(web, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources"));

                CreateThemeEntry(web, "Custom Theme", "ColorPalette.spcolor", "Font.spfont", null);


                //This actually injects the JS Files
                AddScriptLink(ctx, "/SiteAssets/js/jquery-3.1.0.min.js", 7);
                AddScriptLink(ctx, "/SiteAssets/js/main.min.js", 9);

                //Set up Custom Logo
                //web.SiteLogoUrl = web.ServerRelativeUrl + "/SiteAssets/images/logo.png";
                //RemoveCustomScript(ctx, web);

                //Assign Alternate CSS
                web.AlternateCssUrl = ctx.Web.ServerRelativeUrl + "/SiteAssets/css/style.min.css";

                // Update settings at the site level.
                web.Update();
                web.Context.ExecuteQuery();

                // #########################################################################
                // Following part is for different hardware device support, but does also
                // activate site collection scoped publishing feature at site, which is not
                // necessarely always optimal
                // #########################################################################

                // Ensure proper meta tag for viewport is set
                // Make sure that hardware devices scale CSS definitions properly
                // More details can be found http://www.n8d.at/blog/how-to-add-viewport-meta-without-editing-the-master-page/

                // Check if SEO is enabled and property for custom meta tags exists
                // This can be enabled on any team site based site collection by enabling
                // the site collection feature "Publishing Infastructure"
                // No other publishing related feature needs to be activated
                // Enable custom meta tags
                allProperties["seoincludecustommetatagpropertyname"] = true.ToString();
                //// Add value of custom meta tag
                allProperties["seocustommetatagpropertyname"] = "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, maximum-scale=1\" />";

                web.Update();
                web.Context.ExecuteQuery();

                //// get features collection on web
                //FeatureCollection features = web.Features;
                //web.Context.Load(features);
                //web.Context.ExecuteQuery();


                // disable the 'Mobile Browser View' web feature 
                //Guid featureId = new Guid("d95c97f3-e528-4da2-ae9f-32b3535fbb59");
                //if (Enumerable.Any(features, feature => feature.DefinitionId == featureId))
                //{
                //    features.Remove(new Guid("d95c97f3-e528-4da2-ae9f-32b3535fbb59"), false);
                //    web.Context.ExecuteQuery();
                //} 


                /// Uncomment to clear
                // Removes alternate CSS URL
                // web.AlternateCssUrl = "";
                // Clear viewport meta tag form SEO settings
                //if (allProperties.FieldValues.ContainsKey("seoincludecustommetatagpropertyname"))
                //{
                //    allProperties["seoincludecustommetatagpropertyname"] = false.ToString();
                //}
                // Add value of custom meta tag
                //if (allProperties.FieldValues.ContainsKey("seocustommetatagpropertyname"))
                //{
                //    allProperties["seocustommetatagpropertyname"] = "<meta name=\"viewport\" content=\"width=device-width, initial-scale=1, maximum-scale=1\" />";
                //}
                //web.Update();
                //web.Context.ExecuteQuery();
            }
        }

        /// <summary>
        /// Uploads assets to site assets web
        /// </summary>
        /// <param name="web"></param>
        private static void UploadAssets(Web web, string filePath, string fileName, string fileLocation, string fileExt)
        {
            List currentList;
            Folder listFolder = null;
            if (fileLocation == "siteassets") //Put files in Site Assets
            {
                // Ensure site asset library exists and return list
                currentList = web.Lists.EnsureSiteAssetsLibrary();
                web.Context.Load(currentList, l => l.RootFolder);
                listFolder = currentList.RootFolder;
                if (fileExt != "css" && fileExt != "js" && fileExt != "html")
                {
                    if (fileExt != "png")
                    {
                        fileExt = "fonts";
                    }
                    else if (fileExt == "png")
                    {
                        fileExt = "images";
                    }
                    else
                    {
                        fileExt = "aspx";
                    }
                }
                if (!FolderExists(web, "Site Assets", fileExt))
                {
                    CreateFolder(web, "Site Assets", fileExt);
                }

            }
            else //Put files in Theme Folder
            {
                currentList = web.GetCatalog(123);
                // get the theme list
                web.Context.Load(currentList);
                web.Context.ExecuteQuery();
                listFolder = currentList.RootFolder;

                fileExt = "15";
            }

            web.Context.Load(listFolder);
            web.Context.Load(listFolder.Folders);
            web.Context.ExecuteQuery();
            foreach (Folder folder in listFolder.Folders)
            {
                if (folder.Name == fileExt)
                {
                    listFolder = folder;
                    break;
                }
            }

            // Use CSOM to upload the file to Site Assests Library
            FileCreationInformation newFile = new FileCreationInformation
            {
                Content = System.IO.File.ReadAllBytes(filePath),
                Url = fileName,
                Overwrite = true
            };
            Microsoft.SharePoint.Client.File uploadFile = listFolder.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();

        }

        private static void CreateThemeEntry(Web web, string themeName, string colorFilePath, string fontFilePath, string masterPageName)
        {
            // Let's get instance to the composite look gallery
            List themesOverviewList = web.GetCatalog(124);
            web.Context.Load(themesOverviewList);
            web.Context.ExecuteQuery();
            // Do not add duplicate, if the theme is already there
            if (!ThemeEntryExists(web, themesOverviewList, themeName))
            {
                // if web information is not available, load it
                if (!web.IsObjectPropertyInstantiated("ServerRelativeUrl"))
                {
                    web.Context.Load(web);
                    web.Context.ExecuteQuery();
                }
                // Let's create new theme entry. Notice that theme selection is not available from 
                //  UI in personal sites, so this is just for consistency sake
                ListItemCreationInformation itemInfo = new ListItemCreationInformation();
                Microsoft.SharePoint.Client.ListItem item = themesOverviewList.AddItem(itemInfo);
                item["Name"] = themeName;
                item["Title"] = themeName;
                if (!string.IsNullOrEmpty(colorFilePath))
                {
                    item["ThemeUrl"] = URLCombine(web.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", System.IO.Path.GetFileName(colorFilePath)));
                    colorFilePath = item["ThemeUrl"].ToString();
                }

                if (!string.IsNullOrEmpty(fontFilePath))
                {
                    item["FontSchemeUrl"] = URLCombine(web.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", System.IO.Path.GetFileName(fontFilePath)));
                    fontFilePath = item["FontSchemeUrl"].ToString();
                }
                /*if (!string.IsNullOrEmpty(backGroundPath))
                {
                    item["ImageUrl"] = URLCombine(web.ServerRelativeUrl, string.Format("/_catalogs/theme/15/{0}", System.IO.Path.GetFileName(backGroundPath)));
                }*/
                // we use seattle master if anythign else is not set
                if (string.IsNullOrEmpty(masterPageName))
                {
                    item["MasterPageUrl"] = URLCombine(web.ServerRelativeUrl, "/_catalogs/masterpage/seattle.master");
                }
                else
                {
                    item["MasterPageUrl"] = URLCombine(web.ServerRelativeUrl, string.Format("/_catalogs/masterpage/{0}", Path.GetFileName(masterPageName)));
                }
                masterPageName = item["MasterPageUrl"].ToString();

                item["DisplayOrder"] = 11;
                item.Update();
                //Apply Theme
                web.ApplyTheme(colorFilePath, fontFilePath, null, true);
                web.Context.ExecuteQuery();
            }
        }

        private static void AddScriptLink(ClientContext ctx, string file, int seq)
        {

            // Register Custom Action
            var customAction = ctx.Site.UserCustomActions.Add();
            customAction.Location = "ScriptLink";
            customAction.ScriptSrc = "~SiteCollection" + file;
            customAction.Sequence = seq;
            customAction.Update();
            ctx.ExecuteQuery();

            Console.WriteLine("ScriptLink Added : {0}", file);
        }

        private static void RemoveCustomScript(ClientContext ctx, Web web)
        {
            var existingActions = ctx.Site.UserCustomActions;

            ctx.Load(existingActions);

            ctx.ExecuteQuery();

            var actions = existingActions.ToArray();

            foreach (var action in actions)
            {


                action.DeleteObject();
                ctx.Load(action);
                ctx.ExecuteQuery();


            }
        }


        static SecureString GetPassword()
        {
            SecureString sStrPwd = new SecureString();
            try
            {
                Console.Write("SharePoint Password : ");

                for (ConsoleKeyInfo keyInfo = Console.ReadKey(true); keyInfo.Key != ConsoleKey.Enter; keyInfo = Console.ReadKey(true))
                {
                    if (keyInfo.Key == ConsoleKey.Backspace)
                    {
                        if (sStrPwd.Length > 0)
                        {
                            sStrPwd.RemoveAt(sStrPwd.Length - 1);
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                            Console.Write(" ");
                            Console.SetCursorPosition(Console.CursorLeft - 1, Console.CursorTop);
                        }
                    }
                    else if (keyInfo.Key != ConsoleKey.Enter)
                    {
                        Console.Write("*");
                        sStrPwd.AppendChar(keyInfo.KeyChar);
                    }

                }
                Console.WriteLine("");
            }
            catch (Exception e)
            {
                sStrPwd = null;
                Console.WriteLine(e.Message);
            }

            return sStrPwd;
        }

        static string GetUserName()
        {
            string strUserName = string.Empty;
            try
            {
                Console.Write("SharePoint User Name : ");
                strUserName = Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                strUserName = string.Empty;
            }
            return strUserName;
        }

        static string GetSite()
        {
            string siteUrl = string.Empty;
            try
            {
                Console.Write("Give Office365 site URL : ");
                siteUrl = Console.ReadLine();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                siteUrl = string.Empty;
            }
            return siteUrl;
        }

        private static bool ThemeEntryExists(Web web, List themeList, string themeName)
        {

            CamlQuery query = new CamlQuery();
            string camlString = @"
                <View>
                    <Query>                
                        <Where>
                            <Eq>
                                <FieldRef Name='Name' />
                                <Value Type='Text'>{0}</Value>
                            </Eq>
                        </Where>
                     </Query>
                </View>";
            // Let's update the theme name accordingly
            camlString = string.Format(camlString, themeName);
            query.ViewXml = camlString;
            var found = themeList.GetItems(query);
            web.Context.Load(found);
            web.Context.ExecuteQuery();
            if (found.Count > 0)
            {
                return true;
            }
            return false;
        }

        private static string URLCombine(string baseUrl, string relativeUrl)
        {
            if (baseUrl.Length == 0)
                return relativeUrl;
            if (relativeUrl.Length == 0)
                return baseUrl;
            return string.Format("{0}/{1}", baseUrl.TrimEnd(new char[] { '/', '\\' }), relativeUrl.TrimStart(new char[] { '/', '\\' }));
        }

        public static void ProcessDirectory(Web web, string targetDirectory)
        {
            // Process the list of files found in the directory.
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            foreach (string fileName in fileEntries)
            {
                string fileExt = Path.GetFileName(fileName).ToString().Substring(Path.GetFileName(fileName).ToString().LastIndexOf(".") + 1);
                string fileLocation;
                switch (fileExt)
                {
                    case "spcolor":
                        fileLocation = "theme";
                        break;
                    case "spfont":
                        fileLocation = "theme";
                        break;
                    default:
                        fileLocation = "siteassets";
                        break;
                }


                UploadAssets(web, fileName, Path.GetFileName(fileName).ToString(), fileLocation, fileExt);
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
                ProcessDirectory(web, subdirectory);
        }

        private static void CreateFolder(Web web, string listTitle, string folderName)
        {
            var list = web.Lists.GetByTitle(listTitle);
            var folderCreateInfo = new ListItemCreationInformation
            {
                UnderlyingObjectType = FileSystemObjectType.Folder,
                LeafName = folderName
            };
            var folderItem = list.AddItem(folderCreateInfo);
            folderItem.Update();
            web.Context.ExecuteQuery();
        }

        public static bool FolderExists(Web web, string listTitle, string folderUrl)
        {
            var list = web.Lists.GetByTitle(listTitle);
            var folders = list.GetItems(CamlQuery.CreateAllFoldersQuery());
            web.Context.Load(list.RootFolder);
            web.Context.Load(folders);
            web.Context.ExecuteQuery();
            var folderRelativeUrl = string.Format("{0}/{1}", list.RootFolder.ServerRelativeUrl, folderUrl);
            var enumer = Enumerable.Any(folders, folderItem => (string)folderItem["FileRef"] == folderRelativeUrl);
            return Enumerable.Any(folders, folderItem => (string)folderItem["FileRef"] == folderRelativeUrl);
        }
    }

}

