# Set up a basic branding base for Sharepoint #

### Summary ###
This project creates a solid base for branding Sharepoint (applications seen below in Applies to Section). This base includes, metadata viewport addition, resposnsive css, and an empty injected JS file. All the elements are added without alerting the masterpage in order to comply with Microsoft best pratice.

This project was created using Microsoft Office PNP Same of Branding.InjectResponsiveCSS as a template but has been customized to increase the functionality - https://github.com/OfficeDev/PnP/tree/master/Samples/Branding.InjectResponsiveCSS

The Responsive CSS from this project (file - _responsiveSp.scss) was created by taking the css from the blog referenced below and altering to allow the use of SCSS in this project.
* Heather Solomon (SharePoint Experts, Inc) - [Making Seattle master responsive](http://blog.sharepointexperience.com/2015/03/making-seattle-master-responsive/)

More details on the addition of the Viewport can be found at the blog below.
* Stefan Bauer (n8d) - [How to add viewport meta without editing the master page](http://www.n8d.at/blog/how-to-add-viewport-meta-without-editing-the-master-page/)

### Applies to ###
-  SharePoint 2016 on-premises^
-  Office 365 Multi Tenant (MT)
-  Office 365 Dedicated (D)*
-  SharePoint 2013 on-premises*

^To be tested and determined
*Experience might be slightly different, but the same thinking and process applies to on-premises as well.

### Prerequisites ###
The Web Essentials (2013)/Web Compiler (2015) Extension for Visual Studio will need to be downloaded to allow for easy compilation of SCSS and JS download options below
VS 2013 - https://visualstudiogallery.msdn.microsoft.com/56633663-6799-41d7-9df7-0f2a504ca361
VS 2015 - https://visualstudiogallery.msdn.microsoft.com/3b329021-cd7a-4a01-86fc-714c2d05bb6c
To add valid viewport settings to the master page the site collection feature "SharePoint Server Publishing Infrastructure" needs to be activated. No other publishing related feature needs to be activated.
Deactivation of the Mobile Browser View Feature is also recommended 

### Solution ###
Solution | Author(s)
---------|----------
Branding.Base | Karissa Martindale (Cardinal Solution) 

### Version history ###
Version  | Date | Comments
---------| -----| --------
1.0  | July 7th 2015 | Initial release

### Disclaimer ###
**THIS CODE IS PROVIDED *AS IS* WITHOUT WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING ANY IMPLIED WARRANTIES OF FITNESS FOR A PARTICULAR PURPOSE, MERCHANTABILITY, OR NON-INFRINGEMENT.**