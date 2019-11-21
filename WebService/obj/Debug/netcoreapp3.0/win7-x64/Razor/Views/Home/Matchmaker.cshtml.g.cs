#pragma checksum "D:\Matchmaking\WebService\Views\Home\Matchmaker.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "8b705a7cfdcab5f574c9b914434f3e4b1aef4bf8"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_Matchmaker), @"mvc.1.0.view", @"/Views/Home/Matchmaker.cshtml")]
namespace AspNetCore
{
    #line hidden
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.AspNetCore.Mvc.Rendering;
    using Microsoft.AspNetCore.Mvc.ViewFeatures;
#nullable restore
#line 1 "D:\Matchmaking\WebService\Views\_ViewImports.cshtml"
using WebService;

#line default
#line hidden
#nullable disable
#nullable restore
#line 2 "D:\Matchmaking\WebService\Views\_ViewImports.cshtml"
using WebService.Models;

#line default
#line hidden
#nullable disable
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"8b705a7cfdcab5f574c9b914434f3e4b1aef4bf8", @"/Views/Home/Matchmaker.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b63c0d9b2fe33cc0bd0bc644a89b87d6017668ad", @"/Views/_ViewImports.cshtml")]
    public class Views_Home_Matchmaker : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 1 "D:\Matchmaking\WebService\Views\Home\Matchmaker.cshtml"
  
    ViewData["Title"] = "SF OneApp - Matchmaker";

#line default
#line hidden
#nullable disable
            WriteLiteral(@"<!-- Content Row -->
<div class=""row"">
    <!-- Heading row -->
    <div class=""row"">
        <div class=""col-md-12"">
            <h1>Stateless Backend Service</h1>
            <p>A stateless Reliable Service that does a long running job processing task. The service exposes an RPC endpoint using Service Remoting with a single method though a shared remoting interface.</p>
            <ul>
                <li>
                    <a href=""https://docs.microsoft.com/azure/service-fabric/service-fabric-reliable-services-introduction"" target=""_blank"">
                        Learn more about stateless Reliable Services
                    </a>
                </li>
                <li>
                    <a href=""https://docs.microsoft.com/azure/service-fabric/service-fabric-reliable-services-communication-remoting"" target=""_blank"">
                        Learn more about Service Remoting
                    </a>
                </li>
            </ul>
        </div>
    </div>
    <!-- Ser");
            WriteLiteral(@"vice column -->
    <div class=""row"">
        <div class=""panel panel-primary"">
            <div class=""panel-body"">
                <div class=""col-md-12"">
                    <p>The service performs matchmaking on demand. </p>
                    <!-- Button -->
                    <div class=""row"">
                        <div class=""col-md-3"">
                            <div class=""panel panel-default"">
                                <div class=""panel-heading"">
                                    <button class=""btn btn-primary btn-block"" onclick=""matchmake()"" type=""button"" id=""matchmake"" tabindex=""1"">Matchmake</button>
                                </div>
                                <div class=""panel-body"">
                                    <h1 class=""text-center no-margin"" id=""countDisplay"">0</h1>
                                </div>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
     ");
            WriteLiteral("   </div>\r\n    </div>\r\n</div>");
        }
        #pragma warning restore 1998
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.ViewFeatures.IModelExpressionProvider ModelExpressionProvider { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IUrlHelper Url { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.IViewComponentHelper Component { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IJsonHelper Json { get; private set; }
        [global::Microsoft.AspNetCore.Mvc.Razor.Internal.RazorInjectAttribute]
        public global::Microsoft.AspNetCore.Mvc.Rendering.IHtmlHelper<dynamic> Html { get; private set; }
    }
}
#pragma warning restore 1591
