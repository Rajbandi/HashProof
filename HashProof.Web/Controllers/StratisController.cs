using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using HashProof.Core.Models;
using HashProof.Core.Services;
using Microsoft.AspNetCore.Mvc;

namespace HashProof.Web.Controllers
{
    public class StratisController : Controller
    {
        private readonly ProofService _proofService;
        public StratisController(IProofService proofService)
        {
            _proofService = (ProofService)proofService;
        }
        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public JsonResult GeneratePayment(string hash)
        {
            if(!string.IsNullOrWhiteSpace(hash))
            { 
            var proof = _proofService.GenerateProof(hash);
            return Json(new {address = proof.PayAddress.Address, fee = proof.PayAmount });
            }
            else
            {
                return Json(new {});
            }
        }

        [HttpGet]
        public JsonResult GetBlock(string blockid)
        {
            var block = _proofService.GetBlock(blockid);
            return Json(block);
        }

        [HttpGet]
        public JsonResult GetProof(string hash)
        {
            var x = _proofService.GetProof(hash);
            return Json(new
            {
                hash = x.Hash,
                address = x.PayAddress.Address,
                datetime = x.DateCreated,
                status = x.Status,
                blockid = x.BlockId,
                txid = x.TxId,
                blockheight = x.BlockHeight
            });
        }

        [HttpGet]
        public JsonResult CheckFee(string hash)
        {
            var proof = _proofService.CheckFee(hash);
       
            return Json(proof);
        }

        [HttpGet]
        public JsonResult CheckProof(string hash)
        {
            var proof = _proofService.CheckProof(hash);

            return Json(proof);
        }
        [HttpGet]
        public JsonResult GetPending(int? offset, int? limit, string search)
        {
            var proofs = _proofService.GetPendingProofs(offset, limit, search);
            var data = proofs.Proofs.Select(x => new
            {
                hash = x.Hash,
                address = x.PayAddress.Address,
                datetime = x.DateCreated,
                status = x.Status=="PaymentPending"?"Fee Pending":"Pending",
            }).ToList();

            return Json(new { total = proofs.Total, rows = data });
        }

        [HttpGet]
        public JsonResult GetConfirmed(int? offset, int? limit, string search)
        {
            var proofs = _proofService.GetConfirmedProofs(offset, limit, search);
            var data = proofs.Proofs.Select(x => new
            {
                hash = x.Hash,
                address = x.PayAddress.Address,
                datetime = x.DateCreated,
                status = x.Status,
                blockid = x.BlockId,
                txid = x.TxId,
                blockheight = x.BlockHeight
            }).ToList();
            return Json(new {total = proofs.Total, rows=data });
        }

    }
}