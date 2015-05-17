/*
This file in the main entry point for defining grunt tasks and using grunt plugins.
Click here to learn more. http://go.microsoft.com/fwlink/?LinkID=513275&clcid=0x409
*/
module.exports = function (grunt) {
    grunt.initConfig({
        "resx_compile_task": {
            compile: {
                files: {
                    "Sample9_Resources.cs": {
                        path: "Sample9_Resources.resx",
                        namespace: "Redwood.Samples.BasicSamples",
                        className: "Sample9_Resources"
                    }
                }
            }
        }
    });

    grunt.loadNpmTasks("grunt-resx-compile-task");
};