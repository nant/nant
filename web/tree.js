/* Copyright © 2002 Jean-Claude Manoli [jc@manoli.net]
 *
 * This software is provided 'as-is', without any express or implied warranty.
 * In no event will the author(s) be held liable for any damages arising from
 * the use of this software.
 * 
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 
 *   1. The origin of this software must not be misrepresented; you must not
 *      claim that you wrote the original software. If you use this software
 *      in a product, an acknowledgment in the product documentation would be
 *      appreciated but is not required.
 * 
 *   2. Altered source versions must be plainly marked as such, and must not
 *      be misrepresented as being the original software.
 * 
 *   3. This notice may not be removed or altered from any source distribution.
 */ 

var treeSelected = null; //last treeNode clicked

//pre-load tree nodes images
var imgPlus = new Image();
imgPlus.src="treenodeplus.gif";
var imgMinus = new Image();
imgMinus.src="treenodeminus.gif";
var imgDot = new Image();
imgPlus.src="treenodedot.gif";


function findNode(el)
{
// Takes element and determines if it is a treeNode.
// If not, seeks a treeNode in its parents.
	while (el != null)
	{
		if (el.className == "treeNode")
		{
			break;
		}
		else
		{
			el = el.parentNode;
		}
	}
	return el;
}


function clickAnchor(el)
{
// handles click on a TOC link
//
	expandNode(el.parentNode);
	selectNode(el.parentNode);
	el.blur();
}


function selectNode(el)
{
// Un-selects currently selected node, if any, and selects the specified node
//
	if (treeSelected != null)
	{
		setSubNodeClass(treeSelected, 'A', 'treeUnselected');
	}
	setSubNodeClass(el, 'A', 'treeSelected');
	treeSelected = el;
}


function setSubNodeClass(el, nodeName, className)
{
// Sets the specified class name on el's first child that is a nodeName element
//
	var child;
	for (var i=0; i < el.childNodes.length; i++)
	{
		child = el.childNodes[i];
		if (child.nodeName == nodeName)
		{
			child.className = className;
			break;
		}
	}
}


function expandCollapse(el)
{
//	If source treeNode has child nodes, expand or collapse view of treeNode
//
	if (el == null)
		return;	//Do nothing if it isn't a treeNode
		
	var child;
	var imgEl;
	for(var i=0; i < el.childNodes.length; i++)
	{
		child = el.childNodes[i];
		if (child.src)
		{
			imgEl = child;
		}
		else if (child.className == "treeSubnodesHidden")
		{
			child.className = "treeSubnodes";
			imgEl.src = "treenodeminus.gif";
			break;
		}
		else if (child.className == "treeSubnodes")
		{
			child.className = "treeSubnodesHidden";
			imgEl.src = "treenodeplus.gif";
			break;
		}
	}
}


function expandNode(el)
{
//	If source treeNode has child nodes, expand it
//
	var child;
	var imgEl;
	for(var i=0; i < el.childNodes.length; i++)
	{
		child = el.childNodes[i];
		if (child.src)
		{
			imgEl = child;
		}
		if (child.className == "treeSubnodesHidden")
		{
			child.className = "treeSubnodes";
			imgEl.src = "treenodeminus.gif";
			break;
		}
	}
}

function expandAll(el) {
	var child;
	var imgEl;
	for(var i=0; i < el.childNodes.length; i++)
	{
		child = el.childNodes[i];
		if (child.src)
		{
			imgEl = child;
		}
		if (child.className == "treeSubnodesHidden")
		{
			child.className = "treeSubnodes";
			imgEl.src = "treenodeminus.gif";
		}


		if (child.tagName == "DIV") {
			expandAll(child);
		}
	}
}	

function collapseAll(el) {
	var child;
	var imgEl;
	for(var i=0; i < el.childNodes.length; i++)
	{
		child = el.childNodes[i];
		if (child.src)
		{
			imgEl = child;
		}
		if (child.className == "treeSubnodes")
		{
			child.className = "treeSubnodesHidden";
			imgEl.src = "treenodeplus.gif";
			break;
		}

		if (child.tagName == "DIV") {
			collapseAll(child);
		}
	}
}	
