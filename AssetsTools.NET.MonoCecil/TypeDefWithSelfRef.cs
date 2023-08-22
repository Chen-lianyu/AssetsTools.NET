﻿using Mono.Cecil;
using System;
using System.Collections.Generic;
using System.Text;

namespace AssetsTools.NET.Extra
{
    internal struct TypeDefWithSelfRef
    {
        public TypeReference typeRef;
        public TypeDefinition typeDef;

        public Dictionary<string, TypeDefWithSelfRef> typeParamToArg;

        public TypeDefWithSelfRef(TypeReference typeRef)
        {
            this.typeRef = typeRef;
            typeDef = typeRef.Resolve();
            typeParamToArg = new Dictionary<string, TypeDefWithSelfRef>();

            TypeReference tRef = typeRef;

            if (tRef is ArrayType arrType)
            {
                tRef = arrType.ElementType;
            }

            if (tRef is GenericInstanceType genType)
            {
                if (genType.HasGenericArguments)
                {
                    for (int i = 0; i < genType.GenericArguments.Count; i++)
                    {
                        typeParamToArg.Add(typeDef.GenericParameters[i].Name, new TypeDefWithSelfRef(genType.GenericArguments[i]));
                    }
                }
            }
        }

        public void AssignTypeParams(TypeDefWithSelfRef parentTypeDef)
        {
            if (parentTypeDef.typeParamToArg.Count > 0 && typeRef is GenericInstanceType genType)
            {
                for (int i = 0; i < genType.GenericArguments.Count; i++)
                {
                    TypeDefWithSelfRef genTypeRef = new TypeDefWithSelfRef(genType.GenericArguments[i]);
                    if (genTypeRef.typeRef.IsGenericParameter)
                    {
                        if (parentTypeDef.typeParamToArg.TryGetValue(genTypeRef.typeRef.Name, out TypeDefWithSelfRef mappedType))
                        {
                            typeParamToArg[typeDef.GenericParameters[i].Name] = mappedType;
                        }
                    }
                    else
                    {
                        // nested generic, AssignTypeParams should get it
                        genTypeRef.AssignTypeParams(parentTypeDef);
                        typeParamToArg[typeDef.GenericParameters[i].Name] = genTypeRef;
                    }
                }
            }
        }

        public TypeDefWithSelfRef SolidifyType(TypeDefWithSelfRef typeDef)
        {
            typeDef.AssignTypeParams(this);
            if (typeParamToArg.TryGetValue(typeDef.typeRef.Name, out TypeDefWithSelfRef retType))
            {
                return retType;
            }

            return typeDef;
        }

        public static implicit operator TypeDefWithSelfRef(TypeReference typeReference)
        {
            return new TypeDefWithSelfRef(typeReference);
        }
    }
}