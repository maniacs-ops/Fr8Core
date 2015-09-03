﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.UI.WebControls;
using Core.Interfaces;
using Data.Entities;
using Data.Interfaces;
using Data.States;
using StructureMap;

namespace Core.Services
{
    public class ActionList : IActionList
    {
        private readonly IAction _action;

        public ActionList()
        {
            _action = ObjectFactory.GetInstance<IAction>();
        }

        public IEnumerable<ActionListDO> GetAll()
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                return uow.ActionListRepository.GetAll();
            }
        }

        public ActionListDO GetByKey(int curActionListId)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curActionListDO = uow.ActionListRepository.GetByKey(curActionListId);
                if (curActionListDO == null)
                    throw new ArgumentNullException("actionListId");

                return curActionListDO;
            }
        }

        public void AddAction(ActionDO curActionDO, string position)
        {
            if (!curActionDO.ParentActionListID.HasValue)
                throw new NullReferenceException("ActionListId");

            var curActionList = GetByKey(curActionDO.ParentActionListID.Value);
            if (string.IsNullOrEmpty(position) || position.Equals("last", StringComparison.OrdinalIgnoreCase))
                Reorder(curActionList, curActionDO, position);
            else
                throw new NotSupportedException("Unsupported value causing problems for Action ordering in ActionList.");
            curActionList.Actions.Add(curActionDO);
            if (curActionList.CurrentActivity == null)
                curActionList.CurrentActivity =
                    curActionList.Actions.OrderBy(action => action.Ordering).FirstOrDefault();
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                uow.ActionListRepository.Add(curActionList);
                uow.SaveChanges();
            }
        }

        private void Reorder(ActionListDO curActionListDO, ActionDO curActionDO, string position)
        {
            int ordering = curActionListDO.Actions.Select(action => action.Ordering).Max();
            curActionDO.Ordering = ordering + 1;
        }

        //if the list is unstarted, set it to inprocess
        
        //until curActionList is Completed  
        //    if currentActivity is an Action, process it
        //    else it's an ActionList, call recursively
        public void Process(ActionListDO curActionList)
        {

            //We assume that any unstarted ActionList that makes it to here should be put into process
            if (curActionList.ActionListState == ActionListState.Unstarted && curActionList.CurrentActivity!=null) //need to add pending state for asynchronous cases
            {
                SetState(curActionList, ActionListState.Inprocess);
            }

            
            if (curActionList.ActionListState != ActionListState.Inprocess) //need to add pending state for asynchronous cases
            {
                throw new ArgumentException("tried to process an ActionList that was not in state=InProcess");
            }

            if (curActionList.CurrentActivity == null)
            {
                throw new ArgumentException("An ActionList with a null CurrentActivity should not get this far. It should be Completed or Unstarted");
            }

            //main processing loop for the Activities belonging to this ActionList
            while (curActionList.ActionListState == ActionListState.Inprocess)
            {
                try
                {
                        var currentActivity = curActionList.CurrentActivity;

                        //if the current activity is an Action, just process it
                        //if the current activity is iself an ActionList, then recursively call ActionList#Process
                        if (currentActivity is ActionListDO)
                        {
                            Process((ActionListDO)currentActivity);
                        }
                        else
                        {
                            ProcessAction(curActionList);
                        }

        
                }
                catch (Exception ex)
                {
                    SetState(curActionList, ActionListState.Error);
                    throw new Exception(ex.Message);
                }

                
            }
            SetState(curActionList, ActionListState.Completed); //TODO probably need to test for this



        }

        private void SetState(ActionListDO actionListDO, int actionListState)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                actionListDO.ActionListState = actionListState;
                uow.ActionListRepository.Attach(actionListDO);
                uow.SaveChanges();
            }
        }

        public void ProcessAction(ActionListDO curActionList)
        {  
            _action.Process((ActionDO) curActionList.CurrentActivity);
            UpdateActionListState(curActionList);
        }
        

        public void UpdateActionListState(ActionListDO curActionListDO)
        {

            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                //update CurrentActivity pointer
                if (curActionListDO.CurrentActivity is ActionDO)
                {
                    if (((ActionDO)curActionListDO.CurrentActivity).ActionState == ActionState.Completed ||
                        ((ActionDO)curActionListDO.CurrentActivity).ActionState == ActionState.InProcess)
                    {
                        ActionDO actionDO = curActionListDO.Actions.OrderBy(o => o.Ordering)
                            .Where(o => o.Ordering > curActionListDO.CurrentActivity.Ordering)
                            .DefaultIfEmpty(null)
                            .FirstOrDefault();

                        if (actionDO != null)
                            curActionListDO.CurrentActivity = actionDO;
                        else
                        {
                            //we're done, no more activities to process in this list
                            curActionListDO.CurrentActivity = null;
                            curActionListDO.ActionListState = ActionListState.Completed;
                        }
                    }
                    else
                    {
                        throw new Exception(string.Format("Action List ID: {0}. Action status returned: {1}",
                            curActionListDO.Id, ((ActionDO)curActionListDO.CurrentActivity).ActionState));
                    }
                    uow.ActionListRepository.Attach(curActionListDO);
                    uow.SaveChanges();
                }
            }
                
        }
    }
}