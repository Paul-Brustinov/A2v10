﻿/*invoice template*/

import { TRoot, TDocument, TRow } from "model";
import { TRows } from "../invoice/model";

const utils = require('std:utils') as Utils;
const cst = require('std:const');
const du = utils.date;

// common module
const cmn = require('document/common');

const template: Template = {
	properties: {
		'TRoot.$Answer': String,
		'TRow.Sum': cmn.rowSum,
		'TDocument.Sum': cmn.docTotalSum,
		'TDocument.$HasParent'(this: TDocument) { return this.ParentDoc.Id !== 0; },
		'TDocParent.$Name': docParentName,
		'TRoot.$HasInbox'(this: TRoot) { return !!this.Inbox; },
		'TDocument.$Date': {
			get(this: TDocument):Date { return this.Date; },
			set: setDocumentDate
		},
		'TDocument.$FormattedDate': {
			get(this: TDocument): string {
				return du.formatDate(this.Date);
			}
		}
	},
	validators: {
		'Document.Agent': 'Выберите покупателя',
		'Document.DepFrom': 'Выберите склад',
		'Document.Rows[].Entity': 'Выберите товар',
		'Document.Rows[].Price': 'Укажите цену',
		'Document.No': {
			valid(doc: TDocument) { return doc.No > 0; }, msg: 'Invalid document number', severity: cst.SEVERITY.WARNING
		}
	},
	events: {
		'Model.load': modelLoad,
		'Model.saved'(root: TRoot) {
			console.dir(root);
		},
		'Document.Rows[].add': (arr: TRows, row: TRow) => row.Qty = 1,
		'Document.Rows[].Entity.Article.change': cmn.findArticle,
		"Document.Rows[].adding"(arr: TRows, row: TRow) {
			console.dir(row);
		}
	},
	commands: {
		apply: {
			saveRequired: true,
			exec: applyDoc,
			confirm: 'Are you sure?'
		},
		unApply: cmn.docUnApply,
		resumeWorkflow,
		insertAbove: insertRow(InsertTo.above),
		insertBelow: insertRow(InsertTo.below)
	}
};

export default template;

function modelLoad(root: TRoot):void {
	console.dir(root);
	if (root.Document.$isNew)
		cmn.documentCreate(root.Document, 'Waybill');
}

function docParentName(this: TDocument) {
	const doc = this;
	return `№ ${doc.No} от ${du.formatDate(doc.Date)}, ${utils.format(doc.Sum, DataType.Currency)} грн.`;
}

async function resumeWorkflow(this: TRoot) {
	const root = this;
	const vm = this.$vm;
	//alert(root.$Comment);
	let result = await vm.$invoke('resumeWorkflow', { Id: root.Inbox.Id, Answer: root.$Answer }, '/sales/waybill');
	console.dir(result);
	alert('ok');
}

function setDocumentDate(this: TDocument, newDate: Date):void {
	console.dir(this);
	const vm = this.$vm;
	vm.$confirm('are you sure?').then(() => {
		this.Date = newDate;
	});
}

function insertRow(to: InsertTo) {
	return function (this: TRoot, row: TRow) {
		this.Document.Rows.$insert(null, to, row);
	};
}

async function applyDoc(this: TRoot, doc: TDocument) {
	const vm = this.$vm;
	let errs = vm.$getErrors(cst.SEVERITY.ERROR);
	if (errs) {
		vm.$alert({ msg: 'First correct the errors:', list: errs.map(x => x.msg) });
		return;
	}
	errs = vm.$getErrors(cst.SEVERITY.WARNING);
	if (errs) {
		let result = await vm.$confirm({ msg: 'The document has warnings. Apply anyway?', list: errs.map(x => x.msg) });
		if (!result) return;
	}
	alert('apply document here: ' + doc.Id);
}