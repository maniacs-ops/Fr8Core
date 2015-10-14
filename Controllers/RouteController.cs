﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web.Http;
using System.Web.Http.Description;
using AutoMapper;
using Core.Exceptions;
using Core.Interfaces;
using Data.Entities;
using Data.Infrastructure.StructureMap;
using Data.Interfaces;
using Data.Interfaces.DataTransferObjects;
using Data.States;
using Microsoft.AspNet.Identity;
using StructureMap;

namespace Web.Controllers
{
    [fr8ApiAuthorize]
    [RoutePrefix("api/processTemplate")]
    public class ProcessTemplateController : ApiController
    {
        private readonly IRoute _route;
        private readonly ISecurityServices _security;

        public ProcessTemplateController()
            : this(ObjectFactory.GetInstance<IRoute>())
        {
        }

        

        public ProcessTemplateController(IRoute route)
        {
            _route = route;
            _security = ObjectFactory.GetInstance<ISecurityServices>();
        }

        
        [Route("full/{id:int}")]
        [ResponseType(typeof(RouteDTO))]
        [HttpGet]
        public IHttpActionResult GetFullRoute(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var route = uow.RouteRepository.GetByKey(id);
                var result = MapRouteToDTO(route, uow);

                return Ok(result);
            };
        }

        
        // Manual mapping method to resolve DO-1164.
        private RouteDTO MapRouteToDTO(RouteDO curRouteDO, IUnitOfWork uow)
        {
            var subrouteDTOList = uow.SubrouteRepository
                .GetQuery()
                .Include(x => x.Activities)
                .Where(x => x.ParentActivityId == curRouteDO.Id)
                .OrderBy(x => x.Id)
                .ToList()
                .Select((SubrouteDO x) =>
                {
                    var pntDTO = Mapper.Map<FullSubrouteDTO>(x);

                    pntDTO.Actions = Enumerable.ToList(x.Activities.Select(Mapper.Map<ActionDTO>));

                    return pntDTO;
                }).ToList();

            RouteDTO result = new RouteDTO()
            {
                Description = curRouteDO.Description,
                Id = curRouteDO.Id,
                Name = curRouteDO.Name,
                RouteState = curRouteDO.RouteState,
                StartingSubrouteId = curRouteDO.StartingSubrouteId,
                Subroutes = subrouteDTOList
            };

            return result;
        }


        [Route("getactive")]
        [HttpGet]
        public IHttpActionResult GetByStatus(int? id = null, int? status = null)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curRoutes = _route.GetForUser(uow, _security.GetCurrentAccount(uow), _security.IsCurrentUserHasRole(Roles.Admin), id, status);

                if (curRoutes.Any())
                {
                    return Ok(curRoutes.Select(Mapper.Map<RouteOnlyDTO>));
                }
            }

            return Ok();
        }


        // GET api/<controller>
        public IHttpActionResult Get(int? id = null)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                var curRoutes = _route.GetForUser(uow, _security.GetCurrentAccount(uow), _security.IsCurrentUserHasRole(Roles.Admin), id);

                if (curRoutes.Any())
                {
                    // Return first record from curRoutes, in case id parameter was provided.
                    // User intentionally wants to receive a single JSON object in response.
                    if (id.HasValue)
                    {
                        return Ok(Mapper.Map<RouteOnlyDTO>(curRoutes.First()));
                    }

                    // Return JSON array of objects, in case no id parameter was provided.
                    return Ok(curRoutes.Select(Mapper.Map<RouteOnlyDTO>));
                }
            }

            //DO-840 Return empty view as having empty process templates are valid use case.
            return Ok();
        }

        
        
        public IHttpActionResult Post(RouteOnlyDTO processTemplateDto, bool updateRegistrations = false)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                if (string.IsNullOrEmpty(processTemplateDto.Name))
                {
                    ModelState.AddModelError("Name", "Name cannot be null");
                }

                if (!ModelState.IsValid)
                {
                    return BadRequest("Some of the request data is invalid");
                }

                var curRouteDO = Mapper.Map<RouteOnlyDTO, RouteDO>(processTemplateDto, opts => opts.Items.Add("ptid", processTemplateDto.Id));
                var curUserId = _security.GetCurrentUser();

                if (string.IsNullOrWhiteSpace(curUserId))
                {
                    throw new AuthenticationExeception("Unable to resolve current user id.");
                }

                var fr8User = uow.UserRepository.GetQuery().FirstOrDefault(x => x.Id == curUserId);
                if (fr8User == null)
                {
                    throw new AuthenticationExeception("Unable to find fr8 user for current user id");
                }

                curRouteDO.DockyardAccount = fr8User;

                //this will return 0 on create operation because of not saved changes
                _route.CreateOrUpdate(uow, curRouteDO, updateRegistrations);
                uow.SaveChanges();
                processTemplateDto.Id = curRouteDO.Id;
                //what a mess lets try this
                /*curRouteDO.StartingSubroute.Route = curRouteDO;
                uow.SaveChanges();
                processTemplateDto.Id = curRouteDO.Id;*/
                return Ok(processTemplateDto);
            }
        }

        
        [HttpPost]
        [Route("action")]
        [ActionName("action")]
        public IHttpActionResult PutAction(ActionDTO actionDto)
        {
            //A stub until the functionaltiy is ready
            return Ok();
        }

        

        public IHttpActionResult Delete(int id)
        {
            using (var uow = ObjectFactory.GetInstance<IUnitOfWork>())
            {
                _route.Delete(uow, id);

                uow.SaveChanges();
                return Ok(id);
            }
        }

        
        [Route("triggersettings"), ResponseType(typeof(List<ExternalEventDTO>))]
        public IHttpActionResult GetTriggerSettings()
        {
            return Ok("This is no longer used due to V2 Event Handling mechanism changes.");
        }

        
        [Route("activate")]
        public IHttpActionResult Activate(RouteDO curRoute)
        {
            return Ok(_route.Activate(curRoute));
        }

        
        [Route("deactivate")]
        public IHttpActionResult Deactivate(RouteDO curRoute)
        {
            return Ok(_route.Deactivate(curRoute));
        }

        
    }
}