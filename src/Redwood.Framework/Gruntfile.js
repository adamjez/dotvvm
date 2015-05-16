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
                        namespace: "Redwood.Framework.Hosting",
                        className: "ErrorPageTemplate"
                    },
                    "Hosting/FileUploadPageTemplate.cs": {
                        path: "Hosting/FileUploadPageTemplate.tt",
                        namespace: "Redwood.Framework.Hosting",
                        className: "FileUploadPageTemplate"
                    },
                    "ResourceManagement/ClientGlobalize/JQueryGlobalizeRegisterTemplate.cs": {
                        path: "ResourceManagement/ClientGlobalize/JQueryGlobalizeRegisterTemplate.tt",
                        namespace: "Redwood.Framework.ResourceManagement.ClientGlobalize",
                        className: "JQueryGlobalizeRegisterTemplate"
                    }
                }
            }
        }
    });

    grunt.loadNpmTasks("grunt-runtime-t4-template-task");
};