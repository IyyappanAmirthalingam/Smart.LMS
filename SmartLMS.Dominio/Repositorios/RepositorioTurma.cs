﻿using SmartLMS.Dominio.Entidades;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SmartLMS.Dominio.Repositorios
{
    public class RepositorioTurma
    {
        private IContexto _contexto;
        public RepositorioTurma(IContexto contexto)
        {
            _contexto = contexto;
        }


        public IEnumerable<Turma> ListarTurmasPorAluno(Guid idAluno)
        {
            return _contexto.ObterLista<Turma>().Where(t => t.Alunos.Any(a => a.IdAluno == idAluno));
        }
    }
}