/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    grunt.initConfig({
        "runtime_t4_template_task": {
            compile: {
                files: {
                    "Hosting/ErrorPageTemplate.cs": {
                        path: "Hosting/ErrorPageTemplate.tt",
                        namespace: "DotVVM.Framework.Hosting",
                        className: "ErrorPageTemplate"
                    },
                    "Hosting/FileUploadPageTemplate.cs": {
                        path: "Hosting/FileUploadPageTemplate.tt",
                        namespace: "DotVVM.Framework.Hosting",
                        className: "FileUploadPageTemplate"
                    },
                    "ResourceManagement/ClientGlobalize/JQueryGlobalizeRegisterTemplate.cs": {
                        path: "ResourceManagement/ClientGlobalize/JQueryGlobalizeRegisterTemplate.tt",
                        namespace: "DotVVM.Framework.ResourceManagement.ClientGlobalize",
                        className: "JQueryGlobalizeRegisterTemplate"
                    }
                }
            }
        },
        
        "resx_compile_task": {
            compile: {
                files: {
                    "Resources/Controls.Designer.cs": {
                        path: "Resources/Controls.resx",
                        namespace: "DotVVM.Framework.Resources",
                        className: "Controls"
                    },
                    "Resources/Parser.Dothtml.Designer.cs": {
                        path: "Resources/Parser.Dothtml.resx",
                        namespace: "DotVVM.Framework.Resources",
                        className: "Parser_Dothtml"
                    },
                    "Resources/DothtmlParserErrors.Designer.cs": {
                        path: "Resources/DothtmlParserErrors.resx",
                        namespace: "DotVVM.Framework.Resources",
                        className: "DothtmlParserErrors"
                    },
                    "Resources/DothtmlTokenizerErrors.Designer.cs": {
                        path: "Resources/DothtmlTokenizerErrors.resx",
                        namespace: "DotVVM.Framework.Resources",
                        className: "DothtmlTokenizerErrors"
                    }
                }

            }
        }
    });

    grunt.loadNpmTasks("grunt-runtime-t4-template-task");
    grunt.loadNpmTasks("grunt-resx-compile-task");
};