#pragma checksum "D:\Matchmaking\WebService\Views\Home\Requests.cshtml" "{ff1816ec-aa5e-4d10-87f7-6f4963833460}" "c7d3561a5f2ba53cbc4945dfbfdf834df58b7d92"
// <auto-generated/>
#pragma warning disable 1591
[assembly: global::Microsoft.AspNetCore.Razor.Hosting.RazorCompiledItemAttribute(typeof(AspNetCore.Views_Home_Requests), @"mvc.1.0.view", @"/Views/Home/Requests.cshtml")]
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
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"c7d3561a5f2ba53cbc4945dfbfdf834df58b7d92", @"/Views/Home/Requests.cshtml")]
    [global::Microsoft.AspNetCore.Razor.Hosting.RazorSourceChecksumAttribute(@"SHA1", @"b63c0d9b2fe33cc0bd0bc644a89b87d6017668ad", @"/Views/_ViewImports.cshtml")]
    public class Views_Home_Requests : global::Microsoft.AspNetCore.Mvc.Razor.RazorPage<dynamic>
    {
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_0 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("src", new global::Microsoft.AspNetCore.Html.HtmlString("~/lib/signalr/signalr.js"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        private static readonly global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute __tagHelperAttribute_1 = new global::Microsoft.AspNetCore.Razor.TagHelpers.TagHelperAttribute("src", new global::Microsoft.AspNetCore.Html.HtmlString("~/js/requests.js?v=5"), global::Microsoft.AspNetCore.Razor.TagHelpers.HtmlAttributeValueStyle.DoubleQuotes);
        #line hidden
        #pragma warning disable 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperExecutionContext __tagHelperExecutionContext;
        #pragma warning restore 0649
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner __tagHelperRunner = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperRunner();
        #pragma warning disable 0169
        private string __tagHelperStringValueBuffer;
        #pragma warning restore 0169
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __backed__tagHelperScopeManager = null;
        private global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager __tagHelperScopeManager
        {
            get
            {
                if (__backed__tagHelperScopeManager == null)
                {
                    __backed__tagHelperScopeManager = new global::Microsoft.AspNetCore.Razor.Runtime.TagHelpers.TagHelperScopeManager(StartTagHelperWritingScope, EndTagHelperWritingScope);
                }
                return __backed__tagHelperScopeManager;
            }
        }
        private global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper;
        #pragma warning disable 1998
        public async override global::System.Threading.Tasks.Task ExecuteAsync()
        {
#nullable restore
#line 1 "D:\Matchmaking\WebService\Views\Home\Requests.cshtml"
  
    ViewData["Title"] = "SF OneApp - Requests";

#line default
#line hidden
#nullable disable
            WriteLiteral(@"    <div class=""row"">
        <!-- Heading row -->
        <div class=""row"">
            <div class=""col-md-12"">
                <h1>Requests Backend Service</h1>
            </div>
        </div>
        <!-- Input, Table and Messages -->
        <div class=""row"">
            <div class=""panel panel-primary"">
                <div class=""panel-body"">
                    <p>The services store key value pairs in partitions based on the first letter of the key. Add a key value pair to the service below and/or get all the key value pairs from the stateful service.</p>
                    <!-- Input and Table -->
                    <div class=""col-md-6"">
                        <!-- Input -->
                        <div class=""row"">
                            <div class=""col-md-6"">
                                <input type=""text"" class=""form-control"" id=""keyInput"" placeholder=""key"" tabindex=""1"">
                            </div>
                        </div>
                        <div ");
            WriteLiteral(@"class=""row top-buffer"">
                            <div class=""col-md-6"">
                                <button class=""btn btn-primary btn-block"" onclick=""addRequestValue()"" type=""button"" id=""addRequestValue"" tabindex=""3"">Add request</button>
                            </div>
                            <div class=""col-md-6"">
                                <button class=""btn btn-primary btn-block"" onclick=""getAllRequests()"" type=""button"" id=""getAllRequests"" tabindex=""4"">Get All requests in queue</button>
                            </div>
                            <div class=""col-md-6"">
                                <button class=""btn btn-primary btn-block"" onclick=""deleteAllRequests()"" type=""button"" id=""deleteAllRequests"" tabindex=""5"">Delete All requests in queue</button>
                            </div>
                        </div>
                        <!-- Table -->
                        <div class=""row top-buffer"">
                            <div class=""col-md-12 table-resp");
            WriteLiteral("onsive\" style=\"overflow: auto; height:100px\">\r\n                                <table class=\"table table-striped\" id=\"statefulBackendServiceTable\">\r\n                                    <tr>\r\n                                        <th>Key</th>\r\n");
            WriteLiteral(@"                                    </tr>
                                </table>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </div>

        <div class=""row"">
            <div class=""panel panel-primary"">
                <div class=""panel-body"">
                    <div class=""col-md-12"">
                        <p>The service performs matchmaking on demand.</p>
                        <!-- Button -->
                        <div class=""row"">
                            <div class=""col-md-3"">
                                <div class=""panel panel-default"">
                                    <div class=""panel-heading"">
                                        <button class=""btn btn-primary btn-block"" onclick=""matchmake()"" type=""button"" id=""matchmake"" tabindex=""6"">Matchmake</button>
                                    </div>
                                    <div class=""row"">
           ");
            WriteLiteral(@"                             <div class=""col-6"">&nbsp;</div>
                                        <div class=""col-6"">
                                            <ul id=""messagesList""></ul>
                                        </div>
                                    </div>

");
            WriteLiteral("                                </div>\r\n                            </div>\r\n                        </div>\r\n                    </div>\r\n                </div>\r\n            </div>\r\n        </div>\r\n    </div>\r\n\r\n\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "c7d3561a5f2ba53cbc4945dfbfdf834df58b7d928036", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_0);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
            __tagHelperExecutionContext = __tagHelperScopeManager.Begin("script", global::Microsoft.AspNetCore.Razor.TagHelpers.TagMode.StartTagAndEndTag, "c7d3561a5f2ba53cbc4945dfbfdf834df58b7d929075", async() => {
            }
            );
            __Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper = CreateTagHelper<global::Microsoft.AspNetCore.Mvc.Razor.TagHelpers.UrlResolutionTagHelper>();
            __tagHelperExecutionContext.Add(__Microsoft_AspNetCore_Mvc_Razor_TagHelpers_UrlResolutionTagHelper);
            __tagHelperExecutionContext.AddHtmlAttribute(__tagHelperAttribute_1);
            await __tagHelperRunner.RunAsync(__tagHelperExecutionContext);
            if (!__tagHelperExecutionContext.Output.IsContentModified)
            {
                await __tagHelperExecutionContext.SetOutputContentAsync();
            }
            Write(__tagHelperExecutionContext.Output);
            __tagHelperExecutionContext = __tagHelperScopeManager.End();
            WriteLiteral("\r\n");
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
