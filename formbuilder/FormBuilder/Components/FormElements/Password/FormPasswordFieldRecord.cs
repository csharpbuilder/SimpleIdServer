﻿using FormBuilder.Models;
using System.Text.Json.Nodes;

namespace FormBuilder.Components.FormElements.Password;

public class FormPasswordFieldRecord : BaseFormFieldRecord
{
    public string Value 
    { 
        get; set; 
    }

    public bool CanViewPassword
    {
        get; set;
    }

    public override string Type => FormPasswordFieldDefinition.TYPE;

    public override void ExtractJson(JsonObject json)
        => json.Add(Name, Value);

    public override void Apply(JsonNode node)
        => Value = node?.ToString();
}