﻿/// <reference path="../_all.ts" />

/*
    Detail (view/add/edit) controller
*/
module dockyard.controllers {
    'use strict';

    //Setup aliases
    import pwd = dockyard.directives.paneWorkflowDesigner;
    import pdc = dockyard.directives.paneDefineCriteria;
    import psa = dockyard.directives.paneSelectAction;
    import pca = dockyard.directives.paneConfigureAction;
    import pst = dockyard.directives.paneSelectTemplate;
    import pcm = dockyard.directives.paneConfigureMapping;

    class ProcessBuilderController {
        // $inject annotation.
        // It provides $injector with information about dependencies to be injected into constructor
        // it is better to have it close to the constructor, because the parameters must match in count and type.
        // See http://docs.angularjs.org/guide/di


        public static $inject = [
            '$rootScope',
            '$scope',
            'StringService',
            'LocalIdentityGenerator',
            '$state',
            'ActionService',
            '$q'
        ];

        private _scope: interfaces.IProcessBuilderScope;

        constructor(
            private $rootScope: interfaces.IAppRootScope,
            private $scope: interfaces.IProcessBuilderScope,
            private StringService: services.IStringService,
            private LocalIdentityGenerator: services.ILocalIdentityGenerator,
            private $state: ng.ui.IState,
            private ActionService: services.IActionService,
            private $q: ng.IQService
        ) {
            this._scope = $scope;
            this._scope.processTemplateId = $state.params.id;

            this._scope.processNodeTemplates = [];
            this._scope.fields = [
                new model.Field('envelope.name', '[Envelope].Name'),
                new model.Field('envelope.date', '[Envelope].Date')
            ];

            this._scope.currentProcessNodeTemplate = null;

            this._scope.Cancel = angular.bind(this, this.Cancel);
            this._scope.Save = angular.bind(this, this.SaveAction);

            this.setupMessageProcessing();
        }

        /*
            Mapping of incoming messages to handlers
        */
        private setupMessageProcessing() {

            //Process Designer Pane events
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateAdding],
                (event: ng.IAngularEvent, eventArgs: pwd.ProcessNodeTemplateAddingEventArgs) => this.PaneWorkflowDesigner_ProcessNodeTemplateAdding(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.ProcessNodeTemplateSelectingEventArgs) => this.PaneWorkflowDesigner_ProcessNodeTemplateSelecting(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionAdding],
                (event: ng.IAngularEvent, eventArgs: pwd.ActionAddingEventArgs) => this.PaneWorkflowDesigner_ActionAdding(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.ActionSelectingEventArgs) => this.PaneWorkflowDesigner_ActionSelecting(eventArgs));
            this._scope.$on(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_TemplateSelecting],
                (event: ng.IAngularEvent, eventArgs: pwd.TemplateSelectingEventArgs) => this.PaneWorkflowDesigner_TemplateSelecting(eventArgs));

            //Define Criteria Pane events
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_ProcessNodeTemplateRemoving],
                (event: ng.IAngularEvent, eventArgs: pdc.ProcessNodeTemplateRemovingEventArgs) => this.PaneDefineCriteria_ProcessNodeTemplateRemoving(eventArgs));
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Cancelling],
                (event: ng.IAngularEvent) => this.PaneDefineCriteria_Cancelling());
            this._scope.$on(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_ProcessNodeTemplateUpdating],
                (event: ng.IAngularEvent, eventArgs: pdc.ProcessNodeTemplateUpdatingEventArgs) => this.PaneDefineCriteria_ProcessNodeTemplateUpdating(eventArgs));

            //Process Configure Action Pane events
            this._scope.$on(pca.MessageType[pca.MessageType.PaneConfigureAction_ActionUpdated],
                (event: ng.IAngularEvent, eventArgs: pca.ActionUpdatedEventArgs) => this.PaneConfigureAction_ActionUpdated(eventArgs));

            //Process Select Action Pane events
            this._scope.$on(psa.MessageType[psa.MessageType.PaneSelectAction_ActionTypeSelected],
                (event: ng.IAngularEvent, eventArgs: psa.ActionTypeSelectedEventArgs) => this.PaneSelectAction_ActionTypeSelected(eventArgs));
            this._scope.$on(psa.MessageType[psa.MessageType.PaneSelectAction_ActionUpdated],
                (event: ng.IAngularEvent, eventArgs: psa.ActionTypeSelectedEventArgs) => this.PaneSelectAction_ActionUpdated(eventArgs));

            //Process Select Template Pane events
            this._scope.$on(pst.MessageType[pst.MessageType.PaneSelectTemplate_ProcessTemplateUpdated],
                (event: ng.IAngularEvent, eventArgs: pst.ProcessTemplateUpdatedEventArgs) => {
                    this.$state.data.pageSubTitle = eventArgs.processTemplateName
                });
        }
         
        // Find criteria by Id.
        private findProcessNodeTemplate(id: number): model.ProcessNodeTemplate {
            var i;
            for (i = 0; i < this._scope.processNodeTemplates.length; ++i) {
                if (this._scope.processNodeTemplates[i].id === id) {
                    return this._scope.processNodeTemplates[i];
                }
            }
            return null;
        }

        private saveProcessNodeTemplate() {
            if (this._scope.currentProcessNodeTemplate != null) {
                this._scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Save]);
                this._scope.currentProcessNodeTemplate = null;
            }
        }

        /*
            Handles message 'PaneDefineCriteria_ProcessNodeTemplateUpdating'
        */
        private PaneDefineCriteria_ProcessNodeTemplateUpdating(eventArgs: pdc.ProcessNodeTemplateUpdatingEventArgs) {
            console.log('ProcessBuilderController::PaneDefineCriteria_ProcessNodeTemplateUpdating', eventArgs);

            if (eventArgs.processNodeTemplateTempId) {
                this._scope.$broadcast(
                    pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateTempIdReplaced],
                    new pwd.ProcessNodeTemplateTempIdReplacedEventArgs(eventArgs.processNodeTemplateTempId, eventArgs.processNodeTemplateId)
                    );
            }
        }

        /*
            Handles message 'PaneDefineCriteria_CriteriaRemoving'
        */
        private PaneDefineCriteria_ProcessNodeTemplateRemoving(eventArgs: pdc.ProcessNodeTemplateRemovingEventArgs) {
            console.log('ProcessBuilderController::PaneDefineCriteria_ProcessNodeTemplateRemoving', eventArgs);

            // Tell Workflow Designer to remove criteria.
            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateRemoved],
                new pwd.ProcessNodeTemplateRemovedEventArgs(eventArgs.processNodeTemplateId, eventArgs.isTempId)
            );

            // Hide Define Criteria pane.
            this._scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);
        }

        /*
            Handles message 'PaneDefineCriteria_Cancelling'
        */
        private PaneDefineCriteria_Cancelling() {
            console.log('ProcessBuilderController::PaneDefineCriteria_Cancelling');

            // If user worked with temporary (not saved criteria), remove criteria from Workflow Designer.
            if (this._scope.currentProcessNodeTemplate
                && this._scope.currentProcessNodeTemplate.isTempId) {

                this._scope.$broadcast(
                    pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateRemoved],
                    new pwd.ProcessNodeTemplateRemovedEventArgs(
                        this._scope.currentProcessNodeTemplate.id,
                        this._scope.currentProcessNodeTemplate.isTempId
                    )
                );
            }

            // Hide DefineCriteria pane.
            this._scope.$broadcast(
                pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]
                );

            // Set currentCriteria to null, marking that no criteria is currently selected.
            this._scope.currentProcessNodeTemplate = null;
        }

        /*
            Handles message 'PaneDefineCriteria_CriteriaUpdating'
        */
        private PaneDefineCriteria_ProcessNodeTemplateUpdated(eventArgs: pdc.ProcessNodeTemplateRemovingEventArgs) {
            console.log('ProcessBuilderController::PaneDefineCriteria_CriteriaRemoving', eventArgs);

            // Tell Workflow Designer to remove criteria.
            // this._scope.$broadcast(
            //     pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_CriteriaRemoved],
            //     new pwd.CriteriaRemovedEventArgs(eventArgs.criteriaId)
            //     );

            //Added by Alexei Avrutin
            //An event to enable consistency with Design Document (part 3, rule 4)
            var eArgs = new pwd.UpdateCriteriaNameEventArgs(eventArgs.processNodeTemplateId)

            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_UpdateCriteriaName], eArgs);

            // Hide Define Criteria pane.
            this._scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);
        }
            
        /*
            Handles message 'WorkflowDesignerPane_CriteriaAdding'
        */
        private PaneWorkflowDesigner_ProcessNodeTemplateAdding(eventArgs: pwd.ProcessNodeTemplateAddingEventArgs) {
            console.log('ProcessBuilderController::PaneWorkflowDesigner_CriteriaAdding', eventArgs);

            this.saveProcessNodeTemplate();

            // Generate next IDs.
            var processNodeTemplateId = this.LocalIdentityGenerator.getNextId();
            var criteriaId = this.LocalIdentityGenerator.getNextId();

            // Create processNodeTemplate with tempId.
            var processNodeTemplate = new model.ProcessNodeTemplate(
                processNodeTemplateId,
                true,
                this._scope.processTemplateId,
                'New criteria'
                );

            // Add criteria to list.
            this._scope.processNodeTemplates.push(processNodeTemplate);

            // Make Workflow Designer add newly created criteria.
            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ProcessNodeTemplateAdded],
                new pwd.ProcessNodeTemplateAddedEventArgs(processNodeTemplate.clone())
                );
        }

        /*
            Handles message 'WorkflowDesignerPane_CriteriaSelected'
        */
        private PaneWorkflowDesigner_ProcessNodeTemplateSelecting(eventArgs: pwd.ProcessNodeTemplateSelectingEventArgs) {
            console.log("ProcessBuilderController::PaneWorkflowDesigner_CriteriaSelected", eventArgs);

            this.saveProcessNodeTemplate();
            this.SaveAction();

            this._scope.currentAction = null; // the prev action is apparently unselected

            var processNodeTemplate = this.findProcessNodeTemplate(eventArgs.processNodeTemplateId);

            // Set current Criteria to currently selected criteria.
            this._scope.currentProcessNodeTemplate = processNodeTemplate;

            var scope = this._scope;
            // Hide Select Template Pane
            scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Hide]);

            if (processNodeTemplate != null) { // by Aleksei Avrutin: for unit testing
                // Show Define Criteria Pane
                scope.$broadcast(
                    pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Render],
                    new pdc.RenderEventArgs(scope.fields, processNodeTemplate.clone())
                    );
            }

            // Hide Select Action Pane
            scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);
                
            // Hide Configure Action Pane
            scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
        }

        /*
            Handles message 'PaneWorkflowDesigner_ActionAdding'
        */
        private PaneWorkflowDesigner_ActionAdding(eventArgs: pwd.ActionAddingEventArgs) {
            console.log('ProcessBuilderController::PaneWorkflowDesigner_ActionAdding', eventArgs);

            this.saveProcessNodeTemplate();

            // Generate next Id.
            var id = this.LocalIdentityGenerator.getNextId();

            // Create action object.
            var action = new model.Action(
                id,
                true,
                eventArgs.criteriaId
            );

            action.userLabel = 'New Action #' + Math.abs(id).toString();

            // Add action to criteria.
            var processNodeTemplate = this.findProcessNodeTemplate(eventArgs.criteriaId);
            processNodeTemplate.actions.push(action);

            // Add action to Workflow Designer.
            this._scope.$broadcast(
                pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_ActionAdded],
                new pwd.ActionAddedEventArgs(eventArgs.criteriaId, action.clone())
            );
        }

        /*
            Handles message 'WorkflowDesignerPane_ActionSelecting'
        */
        private PaneWorkflowDesigner_ActionSelecting(eventArgs: pwd.ActionSelectingEventArgs) {
            console.log("ProcessBuilderController: action selected");

            this.saveProcessNodeTemplate();
            this.SaveAction();

            //Render Select Action Pane
            var eArgs = new psa.RenderEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                false); // eventArgs.isTempId,

            this._scope.currentAction = this.ActionService.get({ id: eventArgs.criteriaId });

            var scope = this._scope;
            scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Hide]);
                scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);
            scope.$broadcast(
                psa.MessageType[psa.MessageType.PaneSelectAction_Render],
                eArgs
            );
        }

        /*
            Handles message 'WorkflowDesignerPane_TemplateSelected'
        */
        private PaneWorkflowDesigner_TemplateSelecting(eventArgs: pwd.TemplateSelectingEventArgs) {
            console.log("ProcessBuilderController: template selected");
            var scope = this._scope;
            this.SaveAction();
            this._scope.currentAction = null; // action is apparently unselected
            //this._scope.$apply(function () {

            var scope = this._scope;
            //Show Select Template Pane
            var eArgs = new directives.paneSelectTemplate.RenderEventArgs();
            scope.$broadcast(pst.MessageType[pst.MessageType.PaneSelectTemplate_Render]);

            //Hide Define Criteria Pane
            scope.$broadcast(pdc.MessageType[pdc.MessageType.PaneDefineCriteria_Hide]);

            //Hide Select Action Pane
            scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);
                
            //Hide Configure Action Pane
            scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
        }

        /*
            Handles message 'ConfigureActionPane_ActionUpdated'
        */
        private PaneConfigureAction_ActionUpdated(eventArgs: pca.ActionUpdatedEventArgs) {
            //Force update on Select Action Pane (FOR DEMO ONLY, NOT IN DESIGN DOCUMENT)
            var eArgs = new directives.paneSelectAction.UpdateActionEventArgs(
                eventArgs.criteriaId, eventArgs.actionId, eventArgs.isTempId);
            this._scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_UpdateAction], eArgs);
            //Update Action on Designer
            eArgs = new pwd.UpdateActionEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                eventArgs.isTempId,
                null);

            this._scope.$broadcast(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_UpdateAction], eArgs);
        }

        /*
            Handles message 'SelectActionPane_ActionTypeSelected'
        */
        private PaneSelectAction_ActionTypeSelected(eventArgs: psa.ActionTypeSelectedEventArgs) {
            //Render Pane Configure Action 
            var eArgs = new pca.RenderEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                eventArgs.isTempId); //is it a temporary id
                
            this._scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Render], eArgs);

            //Render Pane Configure Mapping 
            eArgs = new pcm.RenderEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                eventArgs.isTempId); //is it a temporary id

            this._scope.$broadcast(pcm.MessageType[pcm.MessageType.PaneConfigureMapping_Render], eArgs);
        }
         
        /*
            Handles message 'PaneSelectAction_ActionUpdated'
        */
        private PaneSelectAction_ActionUpdated(eventArgs: psa.ActionTypeSelectedEventArgs) {
            //Update Pane Workflow Designer
            var eArgs = new pwd.UpdateActionEventArgs(
                eventArgs.criteriaId,
                eventArgs.actionId,
                eventArgs.isTempId,
                eventArgs.actionName);
            this._scope.$broadcast(pwd.MessageType[pwd.MessageType.PaneWorkflowDesigner_UpdateAction], eArgs);
        }

        public SaveAction() {
            //If an action is selected, save it
            if (this._scope.currentAction != null) {
                return this.ActionService.save({
                    id: this._scope.currentAction.id
                }, this._scope.currentAction, null, null).$promise;
            }
        }

        public Cancel() {
            this._scope.currentAction = null;
            this.HideActionPanes();
        }

        private HideActionPanes() {
            //Hide Select Action Pane
            this._scope.$broadcast(psa.MessageType[psa.MessageType.PaneSelectAction_Hide]);

            //Hide Configure Mapping Pane
            this._scope.$broadcast(pcm.MessageType[pcm.MessageType.PaneConfigureMapping_Hide]);

            //Hide Configure Action Pane
            this._scope.$broadcast(pca.MessageType[pca.MessageType.PaneConfigureAction_Hide]);
        }
    }

    app.run([
        "$httpBackend", "urlPrefix", ($httpBackend, urlPrefix) => {
            var actions: interfaces.IAction =
                {
                    actionType: "test action type",
                    configurationSettings: "",
                    criteriaId: 1,
                    id: 1,
                    isTempId: false,
                    fieldMappingSettings: "",
                    userLabel: "test",
                    tempId: 0,
                    actionListId: 0
                };

            $httpBackend
                .whenGET(urlPrefix + "/Action/1")
                .respond(actions);

            $httpBackend
                .whenPOST(urlPrefix + "/Action/1")
                .respond(function (method, url, data) {
                    return data;
                })
    }
    ]);

    app.controller('ProcessBuilderController', ProcessBuilderController);
} 