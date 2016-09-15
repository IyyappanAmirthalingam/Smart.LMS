﻿using Carubbi.Utils.Data;
using Humanizer.DateTimeHumanizeStrategy;
using SmartLMS.Dominio;
using SmartLMS.Dominio.Entidades.Conteudo;
using SmartLMS.Dominio.Entidades.Historico;
using SmartLMS.Dominio.Entidades.Pessoa;
using SmartLMS.Dominio.Repositorios;
using SmartLMS.WebUI.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Transactions;
using System.Web.Mvc;

namespace SmartLMS.WebUI.Controllers
{
    [Authorize]
    public class AulaController : BaseController
    {
        public AulaController(IContexto contexto)
            : base(contexto)
        {

        }

        [HttpPost]
        public ActionResult AtualizarProgresso(AcessoAulaViewModel viewModel)
        {
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            var usuarioRepo = new RepositorioUsuario(_contexto);

            var aulaRepo = new RepositorioAula(_contexto);
            var aula = aulaRepo.ObterAula(viewModel.IdAula, _usuarioLogado.Id);
            acessoRepo.AtualizarProgresso(viewModel.ToEntity(_usuarioLogado, aula.Aula));

            return new HttpStatusCodeResult(HttpStatusCode.OK, "Atualizado");
        }

        public ActionResult Ver(Guid id)
        {
            var aulaRepo = new RepositorioAula(_contexto);
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            var aula = aulaRepo.ObterAula(id, _usuarioLogado.Id);
            if (aula.Disponivel && aula.Aula.Ativo)
            {
                acessoRepo.CriarAcesso(new AcessoAula { Aula = aula.Aula, Usuario = _usuarioLogado, DataHoraAcesso = DateTime.Now, Percentual = aula.Percentual, Segundos = aula.Segundos });
                return View(AulaViewModel.FromEntityComArquivos(aula));
            }
            else
            {
                TempData["TipoMensagem"] = "warning";
                TempData["TituloMensagem"] = "Aviso";
                TempData["Mensagem"] = "Esta aula não está disponível para você";
                return RedirectToAction("Index", "Aula", new { Id = aula.Aula.Curso.Id });
            }

        }

        [AllowAnonymous]
        public ActionResult Index(Guid id)
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var indice = cursoRepo.ObterIndiceCurso(id, _usuarioLogado?.Id);
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            CursoViewModel viewModel = CursoViewModel.FromEntity(indice);
            ViewBag.OutrosCursos = new SelectList(indice.Curso.Assunto.Cursos.Except(new List<Curso> { indice.Curso }), "Id", "Nome");
            return View(viewModel);
        }

        [AllowAnonymous]
        public ActionResult ExibirIndiceCurso(Guid id)
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            var indice = cursoRepo.ObterIndiceCurso(id, _usuarioLogado.Id);
            CursoViewModel viewModel = CursoViewModel.FromEntity(indice);
            return PartialView("_indiceCurso", viewModel);
        }

        [ChildActionOnly]
        public ActionResult ListarAulas(Guid id, Guid idAulaCorrente)
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            var indice = cursoRepo.ObterIndiceCurso(id, _usuarioLogado.Id);
            CursoViewModel viewModel = CursoViewModel.FromEntity(indice);
            ViewBag.IdAulaCorrente = idAulaCorrente;
            return PartialView("_listaAulasPequena", viewModel.Aulas);
        }


        [ChildActionOnly]
        public ActionResult ExibirPainelNovasAulas()
        {


            var aulaRepo = new RepositorioAula(_contexto);
            return PartialView("_PainelNovasAulas", AulaViewModel.FromEntityList(aulaRepo.ListarUltimasAulasLiberadas(_usuarioLogado.Id), new DefaultDateTimeHumanizeStrategy()));
        }

        [ChildActionOnly]
        public ActionResult ExibirUltimasAulas()
        {
            var acessoRepo = new RepositorioAcessoAula(_contexto);
            return PartialView("_PainelUltimasAulas", AcessoAulaViewModel.FromEntityList(acessoRepo.ListarUltimosAcessos(_usuarioLogado.Id), new DefaultDateTimeHumanizeStrategy()));
        }

        [HttpPost]
        public ActionResult ListarComentarios(Guid idAula, int pagina = 1)
        {
            var aulaRepo = new RepositorioAula(_contexto);
            var aula = aulaRepo.ObterAula(idAula, _usuarioLogado.Id);
            var humanizer = new DefaultDateTimeHumanizeStrategy();
            var comentarios = aula.Aula.Comentarios
                .OrderByDescending(x => x.DataHora)
                .Skip(((pagina - 1) * 10))
                .Take(10)
                .ToList();

            return Json(ComentarioViewModel.FromEntityList(comentarios, humanizer, _usuarioLogado.Id), JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Comentar(FormCollection formData)
        {
            if (!string.IsNullOrEmpty(formData["Comentario"]))
            {
                ComentarioViewModel comentario = new ComentarioViewModel
                {
                    IdAula = new Guid(formData["IdAula"]),
                    Comentario = formData["Comentario"]
                };

                var aulaRepo = new RepositorioAula(_contexto);
                var aula = aulaRepo.ObterAula(comentario.IdAula, _usuarioLogado.Id);
                comentario.DataHora = DateTime.Now;
                aulaRepo.Comentar(comentario.ToEntity(_usuarioLogado, aula.Aula));
                return new HttpStatusCodeResult(HttpStatusCode.OK);
            }
            else
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
        }

        [HttpPost]

        public ActionResult ExcluirComentario(long idComentario)
        {
            var aulaRepo = new RepositorioAula(_contexto);

            aulaRepo.ExcluirComentario(idComentario);
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        [AllowAnonymous]
        public ActionResult Baixar(Guid id)
        {

            RepositorioAula aulaRepo = new RepositorioAula(_contexto);
            Arquivo arquivo = aulaRepo.ObterArquivo(id);

            var disponivel = aulaRepo.VerificarDisponibilidadeAula(arquivo.Aula.Id, _usuarioLogado?.Id);
            if (disponivel)
            {
                aulaRepo.GravarAcesso(arquivo, _usuarioLogado);
                return File(Url.Content("~/" + SmartLMS.Dominio.Entidades.Parametro.STORAGE_ARQUIVOS + "/" + arquivo.Aula.Id + "/" + arquivo.ArquivoFisico), "application/octet-stream", arquivo.ArquivoFisico);
            }
            else
            {
                TempData["TipoMensagem"] = "error";
                TempData["TituloMensagem"] = "Download de material de apoio";
                TempData["Mensagem"] = "Você não possui permissão para baixar este material";
                return RedirectToAction("Index", "Home");
            }
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult IndexAdmin(string termo, string campoBusca, int pagina = 1)
        {
            ViewBag.CamposBusca = new SelectList(new string[] { "Nome", "Assunto", "Área de Conhecimento", "Curso", "Id" });
            RepositorioAula repo = new RepositorioAula(_contexto);
            return View(AulaViewModel.FromEntityList(repo.ListarAulas(termo, campoBusca, pagina)));

        }

        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult ListarAulas(string termo, string campoBusca, int pagina = 1)
        {
            RepositorioAula repo = new RepositorioAula(_contexto);
            return Json(AulaViewModel.FromEntityList(repo.ListarAulas(termo, campoBusca, pagina)));
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult Create()
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var cursos = cursoRepo.ListarCursosAtivos();
            ViewBag.Cursos = new SelectList(cursos, "Id", "Nome");

            var usuarioRepo = new RepositorioUsuario(_contexto);
            var professores = usuarioRepo.ListarProfessoresAtivos();
            ViewBag.Professores = new SelectList(professores, "Id", "Nome");
            TipoConteudo tiposAula = TipoConteudo.Vimeo;

            ViewBag.TiposAula = new SelectList(tiposAula.ToDataSource<TipoConteudo>(), "Key", "Value");

            return View();
        }

        // POST: professor/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Administrador")]
        public ActionResult Create(AulaViewModel aula)
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var usuarioRepo = new RepositorioUsuario(_contexto);

            if (ModelState.IsValid)
            {
                try
                {
                    var curso = cursoRepo.ObterPorId(aula.IdCurso);
                    var professor = (Professor)usuarioRepo.ObterPorId(aula.IdProfessor);

                    RepositorioAula repo = new RepositorioAula(_contexto);
                    repo.Incluir(AulaViewModel.ToEntity(aula, curso, professor));

                    TempData["TipoMensagem"] = "success";
                    TempData["TituloMensagem"] = "Administração de conteúdo";
                    TempData["Mensagem"] = "Aula criada com sucesso";
                    return RedirectToAction("IndexAdmin");
                }
                catch (Exception ex)
                {
                    TempData["TipoMensagem"] = "error";
                    TempData["TituloMensagem"] = "Administração de conteúdo";
                    TempData["Mensagem"] = ex.Message;
                }
            }

            TipoConteudo tiposAula = TipoConteudo.Vimeo;
            ViewBag.TiposAula = new SelectList(tiposAula.ToDataSource<TipoConteudo>(), "Key", "Value");

            var cursos = cursoRepo.ListarCursosAtivos();
            ViewBag.Cursos = new SelectList(cursos, "Id", "Nome");
            var professores = usuarioRepo.ListarProfessoresAtivos();
            ViewBag.Professores = new SelectList(professores, "Id", "Nome");
            return View(aula);
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult SalvarMaterialApoio(string id)
        {
            using (TransactionScope tx = new TransactionScope())
            {
                MaterialApoioUploader uploader = new MaterialApoioUploader(id);
                RepositorioAula repo = new RepositorioAula(_contexto);
                var uploadResult = uploader.Upload(Request.Files[0]);
                var aula = repo.ObterPorId(new Guid(id));
                aula.Arquivos.Add(new Arquivo
                {
                    ArquivoFisico = uploadResult.Message,
                    DataCriacao = DateTime.Now,
                    Ativo = true,
                    Nome = Request.Files[0].FileName,
                    Aula = aula
                });
                _contexto.Salvar();
                tx.Complete();
                return Json(uploadResult);
            }
        }

        [Authorize(Roles = "Administrador")]
        public ActionResult RemoverMaterialApoio(string id, string nomeArquivo)
        {
            using (TransactionScope tx = new TransactionScope())
            {
                RepositorioAula repo = new RepositorioAula(_contexto);
                MaterialApoioUploader uploader = new MaterialApoioUploader(id);
                var aula = repo.ObterPorId(new Guid(id));
                var arquivo = aula.Arquivos.Single(x => x.ArquivoFisico == nomeArquivo);
                aula.Arquivos.Remove(arquivo);
                repo.Atualizar(aula);
                uploader.DeleteFile(nomeArquivo);
                tx.Complete();
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }

        public ActionResult ListarMateriaisDeApoio(string idAula)
        {
            RepositorioAula repo = new RepositorioAula(_contexto);
            var aula = repo.ObterPorId(new Guid(idAula));
            List<dynamic> arquivos = new List<dynamic>();
            MaterialApoioUploader uploader = new MaterialApoioUploader(idAula);
            
            foreach (var item in aula.Arquivos)
            {
                arquivos.Add(new { Nome = item.Nome, size = uploader.GetFileInfo(item.ArquivoFisico).Length });
            }

            return Json(new { Arquivos = arquivos });
        }


        [Authorize(Roles = "Administrador")]
        public ActionResult Edit(Guid id)
        {
            var cursoRepo = new RepositorioCurso(_contexto);
            var cursos = cursoRepo.ListarCursosAtivos();
            ViewBag.Cursos = new SelectList(cursos, "Id", "Nome");

            var usuarioRepo = new RepositorioUsuario(_contexto);
            var professores = usuarioRepo.ListarProfessoresAtivos();
            ViewBag.Professores = new SelectList(professores, "Id", "Nome");
            TipoConteudo tiposAula = TipoConteudo.Vimeo;

            ViewBag.TiposAula = new SelectList(tiposAula.ToDataSource<TipoConteudo>(), "Key", "Value");


            var repo = new RepositorioAula(_contexto);
            var aula = repo.ObterPorId(id);
            return View(AulaViewModel.FromEntity(aula, 0, new DefaultDateTimeHumanizeStrategy()));
        }

        [Authorize(Roles = "Administrador")]
        [HttpPost]
        public ActionResult Edit(Guid id, AulaViewModel viewModel)
        {

            var cursoRepo = new RepositorioCurso(_contexto);
            var usuarioRepo = new RepositorioUsuario(_contexto);

            if (ModelState.IsValid)
            {
                try
                {
                    var curso = cursoRepo.ObterPorId(viewModel.IdCurso);
                    var professor = (Professor)usuarioRepo.ObterPorId(viewModel.IdProfessor);
                    var repo = new RepositorioAula(_contexto);
                    repo.Alterar(AulaViewModel.ToEntity(viewModel, curso, professor));
                    TempData["TipoMensagem"] = "success";
                    TempData["TituloMensagem"] = "Administração de conteúdo";
                    TempData["Mensagem"] = "Aula alterada com sucesso";
                    return RedirectToAction("IndexAdmin");
                }
                catch (Exception ex)
                {
                    TempData["TipoMensagem"] = "error";
                    TempData["TituloMensagem"] = "Administração de conteúdo";
                    TempData["Mensagem"] = ex.Message;
                }
            }


            var cursos = cursoRepo.ListarCursosAtivos();
            ViewBag.Cursos = new SelectList(cursos, "Id", "Nome");

            var professores = usuarioRepo.ListarProfessoresAtivos();
            ViewBag.Professores = new SelectList(professores, "Id", "Nome");
            TipoConteudo tiposAula = TipoConteudo.Vimeo;

            ViewBag.TiposAula = new SelectList(tiposAula.ToDataSource<TipoConteudo>(), "Key", "Value");

            return View(viewModel);
        }



        [HttpPost]
        [Authorize(Roles = "Administrador")]
        public ActionResult Excluir(string id)
        {
            RepositorioAula repo = new RepositorioAula(_contexto);
            MaterialApoioUploader uploader = new MaterialApoioUploader(id);
            var aula = repo.ObterPorId(new Guid(id));
            using (TransactionScope tx = new TransactionScope())
            {
                foreach (var arquivo in aula.Arquivos)
                {
                    uploader.DeleteFile(arquivo.ArquivoFisico);
                }
                    
                repo.Excluir(aula.Id);
                tx.Complete();
            }
            return new HttpStatusCodeResult(HttpStatusCode.OK);
        }
    }
}
