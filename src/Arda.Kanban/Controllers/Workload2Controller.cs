﻿using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using Newtonsoft.Json;
using System.Net;
using Arda.Kanban.Models.Repositories;
using Arda.Kanban.ViewModels;
using System.Linq;

namespace Arda.Kanban.Controllers
{
    public class AssignInput
    {
        public string tag { get; set; }
        public string wbid { get; set; }
    }

    [Route("api/[controller]")]
    public class Workload2Controller : Controller
    {
        IWorkloadRepository _repository;
        Repositories.WorkloadExtraRepository _extraRepo;

        Models.KanbanContext _context;

        public Workload2Controller(IWorkloadRepository repository, Models.KanbanContext context)
        {
            _repository = repository;
            _context = context;
            _extraRepo = new Repositories.WorkloadExtraRepository(context);
        }

        [HttpGet]
        [Route("listtag")] // replace with /workspace/default/search/<tag>
        public IEnumerable<WorkloadsByUserViewModel> ListTag([FromQuery]string tag)
        {
            return _extraRepo.GetWorkloads(tag);
        }

        [HttpGet]
        [Route("{tag}")] // replace with /workspace/default/items/search/<tag>
        public IEnumerable<WorkloadsByUserViewModel> GetTag(string tag)
        {
            return _extraRepo.GetWorkloads(tag);
        }

        [HttpGet]
        [Route("liststatus/{tag}")] // replace with /workspace/default/search/<tag>?$status
        public IEnumerable<WorkloadStatusCompatViewModel> GetStatus(string tag)
        {
            var workloadList = _extraRepo.GetWorkloadStatus(tag);

            var result = (from w in workloadList
                          select new WorkloadStatusCompatViewModel()
                          {
                              _WorkloadID = w.WorkloadID,
                              _WorkloadTitle = w.Title,
                              _WorkloadStatus = w.State,
                              _WorkloadUsers = (from user in w.Users
                                                select new Tuple<string, string>(user, user)).ToArray(),
                              StatusText = w.StatusText,

                              _WorkloadIsWorkload = true
                          }
                          ).ToArray();

            return result;
        }

        [HttpPost]
        [Route("{tag}/assign/{wbid}")] // replace with PUT /workspace/<workspaceId>/items/<guid>/tags
        public bool Assign([FromRoute]string tag, [FromRoute]string wbid)
        {
            _extraRepo.AssignTag(Guid.Parse(wbid), tag);

            return true;
        }

        [HttpGet]
        [Route("listworkloadwithfilter")]
        public IEnumerable<WorkloadsByUserViewModel> ListWorkloadWithFilter([FromQuery]string tag)
        {
            return _extraRepo.GetWorkloads(tag);
        }
        
        [HttpPost]
        [Route("{tag}/add")]
        public HttpResponseMessage Add([FromRoute]string tag)
        {
            // var uniqueName = HttpContext.Request.Headers["unique_name"].ToString();

            System.IO.StreamReader reader = new System.IO.StreamReader(HttpContext.Request.Body);
            string requestFromPost = reader.ReadToEnd();
            var workload = JsonConvert.DeserializeObject<WorkloadViewModel>(requestFromPost);

            // Calling add
            var response = _repository.AddNewWorkload(workload);

            _extraRepo.AssignTag(workload.WBID, tag);

            if (response)
            {
                return new HttpResponseMessage(HttpStatusCode.OK);
            }
            else
            {
                return new HttpResponseMessage(HttpStatusCode.InternalServerError);
            }
        }
    }
}
