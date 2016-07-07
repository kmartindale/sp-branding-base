﻿using System.Linq;
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
                ctx.Credentials = new SharePointOnlineCredentials(userName, pwd);

                Web web = ctx.Web;
                var allProperties = web.AllProperties;

                ctx.Load(web);
                ctx.Load(allProperties);
                ctx.ExecuteQuery();


                UploadAssetToHostWeb(web, "style.min.css");
                UploadAssetToHostWeb(web, "main.min.js");

                AddScriptLink(ctx, "/SiteAssets/main.min.js", 9);

                // Actual code for operations
                // Set the properties accordingly
                // Notice that these are new properties in 2014 April CU of 15 hive CSOM and July release of MSO CSOM
                web.AlternateCssUrl = ctx.Web.ServerRelativeUrl + "SiteAssets/style.min.css";
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
                /*
                    FeatureCollection features = web.Features;
                    web.Context.Load(features);
                    web.Context.ExecuteQuery();
                */


                // disable the 'Mobile Browser View' web feature 
                /*
                    Guid featureId = new Guid("d95c97f3-e528-4da2-ae9f-32b3535fbb59");
                    if (Enumerable.Any(features, feature => feature.DefinitionId == featureId))
                    {
                        features.Remove(new Guid("d95c97f3-e528-4da2-ae9f-32b3535fbb59"), false);
                        web.Context.ExecuteQuery();
                    }
                */ 


                /// Uncomment to clear
                // Removes alternate CSS URL
                /*
                    web.AlternateCssUrl = "";
                    //Clear viewport meta tag form SEO settings
                    allProperties["seoincludecustommetatagpropertyname"] = false.ToString();

                    web.Update();
                    web.Context.ExecuteQuery();
                */
            }
        }

        /// <summary>
        /// Uploads assets to host web
        /// </summary>
        /// <param name="web"></param>
        private static void UploadAssetToHostWeb(Web web, string fileName)
        {
            // Ensure site asset library exists and return list
            List assetLibrary = web.Lists.EnsureSiteAssetsLibrary();
            web.Context.Load(assetLibrary, l => l.RootFolder);

            //Set up Resources Directory as our main Path
            string fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "resources/");

            // Get the path to the file which we are about to deploy
            string filePath = fileFolder + fileName;

            // Use CSOM to upload the file to Site Assests Library
            FileCreationInformation newFile = new FileCreationInformation
            {
                Content = System.IO.File.ReadAllBytes(filePath),
                Url = fileName,
                Overwrite = true
            };
            Microsoft.SharePoint.Client.File uploadFile = assetLibrary.RootFolder.Files.Add(newFile);
            web.Context.Load(uploadFile);
            web.Context.ExecuteQuery();

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

    }
}

