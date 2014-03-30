﻿using System;
using System.Collections.Generic;
using System.Linq;
using Mercraft.Core.Interactions;
using Mercraft.Core.MapCss.Domain;
using Mercraft.Core.Scene;
using Mercraft.Core.Scene.Models;
using Mercraft.Core.Tiles;
using Mercraft.Infrastructure.Diagnostic;
using UnityEngine;

namespace Mercraft.Core.Zones
{
    public class Zone
    {
        private readonly Tile _tile;
        private readonly Stylesheet _stylesheet;
        private readonly IEnumerable<ISceneModelVisitor> _sceneModelVisitors;
        private readonly IEnumerable<IBehaviour> _behaviours;

        private readonly ITrace _trace;

        public Zone(Tile tile,
            Stylesheet stylesheet,
            IEnumerable<ISceneModelVisitor> sceneModelVisitors,
            IEnumerable<IBehaviour> behaviours,
            ITrace trace)
        {
            _tile = tile;
            _stylesheet = stylesheet;
            _sceneModelVisitors = sceneModelVisitors;
            _behaviours = behaviours;
            _trace = trace;
        }

        /// <summary>
        /// Builds game objects
        /// </summary>
        /// <param name="loadedElementIds">Contains ids of previously loaded elements. Used to prevent duplicates</param>
        public void Build(HashSet<long> loadedElementIds)
        {
            // TODO refactor this logic

            var canvas = _tile.Scene.Canvas;
            var canvasRule = _stylesheet.GetRule(canvas);
            GameObject canvasObject = null;

            // visit canvas
            foreach (var sceneModelVisitor in _sceneModelVisitors)
            {
                canvasObject = sceneModelVisitor.VisitCanvas(_tile.RelativeNullPoint, null, canvasRule, canvas);
                if (canvasObject != null)
                    break;
            }

            // TODO probably, we need to return built game object 
            // to be able to perform cleanup on our side
            BuildAreas(canvasObject, loadedElementIds);
            BuildWays(canvasObject, loadedElementIds);
        }

        private void BuildAreas(GameObject parent, HashSet<long> loadedElementIds)
        {
            foreach (var area in _tile.Scene.Areas)
            {
                if (loadedElementIds.Contains(area.Id))
                    continue;

                var rule = _stylesheet.GetRule(area);
                if (rule != null)
                {
                    GameObject areaGameObject = null;
                    foreach (var sceneModelVisitor in _sceneModelVisitors)
                    {
                        areaGameObject = sceneModelVisitor.VisitArea(_tile.RelativeNullPoint, parent, rule, area)
                                         ?? areaGameObject;
                    }
                    ApplyBehaviour(areaGameObject, area, rule);
                    loadedElementIds.Add(area.Id);
                }
                else
                {
                    _trace.Warn(String.Format("No rule for area: {0}, points: {1}", area, area.Points.Length));
                }
            }
        }

        private void BuildWays(GameObject parent, HashSet<long> loadedElementIds)
        {
            foreach (var way in _tile.Scene.Ways)
            {
                if (loadedElementIds.Contains(way.Id))
                    continue;

                var rule = _stylesheet.GetRule(way);
                if (rule != null)
                {
                    GameObject wayGameObject = null;
                    foreach (var sceneModelVisitor in _sceneModelVisitors)
                    {
                        wayGameObject = sceneModelVisitor.VisitWay(_tile.RelativeNullPoint, parent, rule, way) ??
                                        wayGameObject;
                    }
                    ApplyBehaviour(wayGameObject, way, rule);
                    loadedElementIds.Add(way.Id);
                }
                else
                {
                    _trace.Warn(String.Format("No rule for way: {0}, points: {1}", way, way.Points.Length));
                }
            }
        }

        private void ApplyBehaviour(GameObject target, Model model, Rule rule)
        {
            // TODO hardcoded string in Core project isn't proper solution
            var behaviourName = rule.EvaluateDefault(model, "behaviour", "");
            if (behaviourName == "")
                return;

            var behaviour = _behaviours.Single(b => b.Name == behaviourName);

            behaviour.Apply(target);
        }
    }
}
