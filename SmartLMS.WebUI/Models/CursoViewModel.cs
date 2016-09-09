﻿using SmartLMS.Dominio.Entidades.Conteudo;
using SmartLMS.Dominio.Repositorios;
using System;
using System.Collections.Generic;
using System.Linq;
using SmartLMS.Dominio.Entidades.Liberacao;
using Carubbi.GenericRepository;

namespace SmartLMS.WebUI.Models
{
    public class CursoViewModel
    {
     
        public string NomeAssunto { get; set; }
        public Guid IdAssunto { get; set; }

        public int Ordem { get; set; }

        public string Nome { get; set; }

        public Guid Id { get; set; }

        public bool Ativo { get; set; }


        public DateTime DataCriacao { get; set; }

        public string Imagem { get; set; }

        public string NomeProfessorResponsavel { get; set; }

        internal static IEnumerable<CursoViewModel> FromEntityList(IEnumerable<Curso> cursos, int profundidade)
        {
            foreach (var item in cursos)
            {
                yield return FromEntity(item, profundidade);
            }
        }

        internal static PagedListResult<CursoViewModel> FromEntityList(PagedListResult<Curso> cursos)
        {
            PagedListResult<CursoViewModel> pagina = new PagedListResult<CursoViewModel>();

            pagina.HasNext = cursos.HasNext;
            pagina.HasPrevious = cursos.HasPrevious;
            pagina.Count = cursos.Count;
            List<CursoViewModel> viewModels = new List<CursoViewModel>();
            foreach (var item in cursos.Entities)
            {
                viewModels.Add(FromEntity(item, 0));
            }

            pagina.Entities = viewModels;
            return pagina;
        }

        public static CursoViewModel FromEntity(Curso item, int profundidade)
        {
            return new CursoViewModel
            {
                Ativo = item.Ativo,
                DataCriacao = item.DataCriacao,
                IdAssunto = item.Assunto.Id,
                NomeAssunto = item.Assunto.Nome,
                Imagem = item.Imagem,
                Ordem = item.Ordem,
                Nome = item.Nome,
                Id = item.Id,
                NomeProfessorResponsavel = item.ProfessorResponsavel.Nome,
                Aulas = profundidade > 2
                ? AulaViewModel.FromEntityList(item.Aulas.Where(a => a.Ativo).OrderBy(x => x.Ordem), profundidade) 
                : new List<AulaViewModel>()
            };
        }

        internal static IEnumerable<CursoViewModel> FromEntityList(List<TurmaCurso> cursos)
        {
            foreach (var item in cursos.OrderBy(x => x.Ordem))
            {
                yield return FromEntity(item);
            }
        }

        private static CursoViewModel FromEntity(TurmaCurso item)
        {
            return new CursoViewModel
            {
                Nome = item.Curso.Nome,
                Id = item.Curso.Id,
                Ordem = item.Ordem
            };
        }

        internal static CursoViewModel FromEntity(IndiceCurso indice)
        {
            return new CursoViewModel
            {
                IdAssunto = indice.Curso.Assunto.Id,
                Imagem = indice.Curso.Imagem,
                Ordem = indice.Curso.Ordem,
                Nome = indice.Curso.Nome,
                Id = indice.Curso.Id,
                NomeProfessorResponsavel = indice.Curso.ProfessorResponsavel.Nome,
                Aulas = AulaViewModel.FromEntityList(indice.AulasInfo)
            };
        }

        public IEnumerable<AulaViewModel> Aulas { get; set; }

    }
}