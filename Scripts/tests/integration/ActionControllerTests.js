/// <reference path="../../app/_all.ts" />
/// <reference path="../../typings/angularjs/angular-mocks.d.ts" />
var dockyard;
(function (dockyard) {
    var tests;
    (function (tests) {
        var controller;
        (function (controller) {
            var fx = tests.utils.fixtures; // just an alias
            describe("Action Controller ", function () {
                var testData = {};
                var errorHandler = function (response, done) {
                    if (response.status === 401) {
                        console.log("User is not logged in, to run these tests, please login");
                    }
                    else {
                        console.log("Something went wrong");
                        console.log(response);
                    }
                    done.fail(response.responseText);
                };
                describe("Action#Get,Delete,Save", function () {
                    var returnedData = null;
                var deleteInvoker = function (data, done) {
                    $.ajax({
                        type: "Delete",
                        url: "/actions/" + data.id,
                        contentType: "application/json; charset=utf-8",
                        dataType: "json"
                    }).done(function (data, status) {
                        console.log("Deleted Successfully id " + data);
                        done();
                    }).fail(function (response) {
                        errorHandler(response, done);
                    });
                };
                var getInvoker = function (data, done) {
                    $.ajax({
                        type: "GET",
                        url: "/actions/" + data[0].id,
                        contentType: "application/json; charset=utf-8",
                        dataType: "json"
                    }).done(function (data, status) {
                        returnedData = data;
                        console.log("Got it Sucessfully");
                        console.log(returnedData);
                        deleteInvoker(data, done);
                    }).fail(function (response) {
                        errorHandler(response, done);
                    });
                };
                var postInvoker = function (done, dataToSave) {
                    $.ajax({
                        type: "POST",
                        url: "/actions/save",
                        contentType: "application/json; charset=utf-8",
                        data: JSON.stringify(dataToSave),
                        dataType: "json"
                    }).done(function (data, status) {
                        console.log("Saved it Sucessfully");
                        console.log(data);
                        getInvoker(data, done);
                    }).fail(function (response) {
                        errorHandler(response, done);
                    });
                };
                beforeEach(function (done) {
                    // First POST, create a dummy entry
                    var actions = {
                        name: "test action type",
                            configurationStore: fx.ActionDesignDTO.configurationStore,
                        processNodeTemplateId: 1,
                        actionTemplateId: 1,
                        isTempId: false,
                        id: 0,
                            fieldMappingSettings: fx.ActionDesignDTO.fieldMappingSettings,
                            // ActionListId is set to null, since there is no ActionsLists on a blank db.
                            actionListId: null,
                            actionTemplate: new dockyard.model.ActionTemplate(1, "Write to SQL", "1")
                    };
                    postInvoker(done, actions);
                });
                it("can save action", function () {
                    expect(returnedData).not.toBe(null);
                });
            });
                describe("Action#GetConfigurationSettings", function () {
                    var endpoint = "/actions";
                    var currentActionDesignDTO = {
                        name: "test action type",
                        configurationStore: fx.ActionDesignDTO.configurationStore,
                        processNodeTemplateId: 1,
                        actionTemplateId: 1,
                        isTempId: false,
                        id: 1,
                        fieldMappingSettings: fx.ActionDesignDTO.fieldMappingSettings,
                        // ActionListId is set to null, since there is no ActionsLists on a blank db.
                        actionListId: null,
                        actionTemplate: fx.ActionTemplate.actionTemplateDO
                    };
                    beforeAll(function () {
                        $(document).ajaxError(errorHandler);
                        $.ajaxSetup({ async: false, url: endpoint, dataType: "json", contentType: "text/json" });
                    });
                    it("Should get the correct configuration settings (AzureSqlServerPluginRegistration_v1)", function () {
                        var expectedSettings = JSON.stringify({ "fields": [{ "name": "connection_string", "required": true, "value": "", "fieldLabel": "SQL Connection String", "type": "textField", "selected": false }], "data-fields": [] });
                        $.ajax({
                            type: "POST",
                            url: "/actions/actions/configuration",
                            contentType: "application/json; charset=utf-8",
                            data: JSON.stringify(currentActionDesignDTO),
                            dataType: "json"
                        }).done(function (data, status) {
                            expect(data).not.toBe(null);
                            expect(angular.equals(data, expectedSettings)).toBe(true);
                        });
                    });
                });
            });
        })(controller = tests.controller || (tests.controller = {}));
    })(tests = dockyard.tests || (dockyard.tests = {}));
})(dockyard || (dockyard = {}));
//# sourceMappingURL=ActionControllerTests.js.map