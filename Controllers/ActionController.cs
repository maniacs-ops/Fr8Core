﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using Microsoft.AspNet.Identity;
using Newtonsoft.Json;
using StructureMap;
using Core.Interfaces;
using Core.Managers;
using Data.Entities;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;

namespace Web.Controllers
{
    [RoutePrefix("actions")]
    public class ActionController : ApiController
    {
        private readonly IAction _action;
        private readonly IActionTemplate _actionTemplate;

        public ActionController()
        {
            _action = ObjectFactory.GetInstance<IAction>();
            _actionTemplate = ObjectFactory.GetInstance<IActionTemplate>();
        }

        public ActionController(IAction service)
        {
            _action = service;
        }

        [Route("configure")]
        [Route("process")]
        [HttpGet]
        public string HandleDockyardRequest(ActionDTO actionDTO)
        {
            // Extract from current request URL.
            var curActionPath = ActionContext.Request.RequestUri.LocalPath.Substring("/actions/".Length);
            var curActionDO = Mapper.Map<ActionDO>(actionDTO);

            var curAssemblyName = string.Format("CoreActions.{0}_v{1}",
                curActionDO.ActivityTemplate.Name,
                curActionDO.ActivityTemplate.Version);

            var calledType = Type.GetType(curAssemblyName);
            var curMethodInfo = calledType
                .GetMethod(curActionPath, BindingFlags.Default | BindingFlags.IgnoreCase);
            var curObject = Activator.CreateInstance(calledType);

            return JsonConvert.SerializeObject(
                (object)curMethodInfo.Invoke(curObject, new Object[] { curActionDO }) ?? new { });
        }

        /// <summary>
        /// GET : Returns an action with the specified id
        /// </summary>
        [HttpGet]
        [Route("{id:int}")]
        public ActionDTO Get(int id)
        {
            return Mapper.Map<ActionDTO>(_action.GetById(id));
        }

        /// <summary>
        /// GET : Returns an action with the specified id
        /// </summary>
        [HttpDelete]
        [Route("{id:int}")]
        public void Delete(int id)
        {
            _action.Delete(id);
        }

        /// <summary>
        /// POST : Saves or updates the given action
        /// </summary>
        [HttpPost]
        [Route("save")]
        public IEnumerable<ActionDTO> Save(ActionDTO curActionDesignDTO)
        {
            ActionDO curActionDO = Mapper.Map<ActionDO>(curActionDesignDTO);
            if (_action.SaveOrUpdateAction(curActionDO))
            {
                curActionDesignDTO.Id = curActionDO.Id;
                return new List<ActionDTO> { curActionDesignDTO };
            }
            return new List<ActionDTO>();
        }

        [HttpPost]
        [Route("configuration")]
        [ResponseType(typeof(CrateStorageDTO))]
        public IHttpActionResult GetConfigurationSettings(ActionDTO curActionDesignDTO)
        {
            ActionDO curActionDO = Mapper.Map<ActionDO>(curActionDesignDTO);
            return Ok(_action.GetConfigurationSettings(curActionDO));  
        }


        /// <summary>
        /// Retrieve the list of data sources for the drop down list boxes on the left side of the field mapping pane in process builder.
        /// </summary>
        [HttpPost]
        [Route("field_data_sources")]
        public IEnumerable<string> GetFieldDataSources(ActionDTO curActionDesignDTO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curAction = uow.ActionRepository.GetByKey(curActionDesignDTO.Id);

                return _action.GetFieldDataSources(uow, curAction);
            }
        }

        /// <summary>
        /// Retrieve the list of data sources for the text labels on the  right side of the field mapping pane in process builder.
        /// </summary>
        [HttpPost]
        [Route("field_mapping_targets")]
        public string GetFieldMappingTargets(ActionDTO curActionDesignDTO)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curAction = uow.ActionRepository.GetByKey(curActionDesignDTO.Id);

                //Field mapping targets are as part of Confgiuration Store of Action DO
                return _action.GetConfigurationSettings(curAction);
            }
        }
    }
}