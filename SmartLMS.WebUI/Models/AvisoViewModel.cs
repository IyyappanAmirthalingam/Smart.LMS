﻿using System;
using System.Collections.Generic;
using SmartLMS.Dominio.Entidades;
using Humanizer.DateTimeHumanizeStrategy;
using System.Globalization;

namespace SmartLMS.WebUI.Models
{
    public class AvisoViewModel
    {
        public long Id { get; set; }
        public bool MensagemDireta { get; set; }

        public string DataHora { get; set; }

        public DateTime? DataTurma { get; set; }

        public string DataTurmaTexto { get {
                return DataTurma.HasValue? DataTurma.Value.ToShortDateString() : string.Empty;
            }
        }

        public string Texto { get; set; }

        internal static IEnumerable<AvisoViewModel> FromEntityList(IEnumerable<Aviso> avisos, DefaultDateTimeHumanizeStrategy humanizer)
        {
            foreach (var item in avisos)
            {
                yield return FromEntity(item, humanizer);
            }
        }

        internal static AvisoViewModel FromEntity(Aviso item, DefaultDateTimeHumanizeStrategy humanizer)
        {
            return new AvisoViewModel
            {
                Id = item.Id,
                DataHora = humanizer.Humanize(item.DataHora, DateTime.Now, CultureInfo.CurrentUICulture),
                Texto = item.Texto,
                DataTurma = item.Turma != null ? item.Turma.DataInicio : (DateTime?)null,
                MensagemDireta = item.Usuario != null,
            };
        }
    }
}