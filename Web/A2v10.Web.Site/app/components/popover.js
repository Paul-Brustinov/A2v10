﻿// Copyright © 2015-2019 Alex Kukhtin. All rights reserved.

//20190906-7554
/*components/popover.js*/

Vue.component('popover', {
	template: `
<div v-dropdown class="popover-wrapper" :style="{top: top}" :class="{show: isShowHover}">
	<span toggle class="popover-title" v-on:mouseover="mouseover" v-on:mouseout="mouseout"><i v-if="hasIcon" :class="iconClass"></i> <span :title="title" v-text="content"></span><slot name="badge"></slot></span>
	<div class="popup-body" :style="{width: width}">
		<div class="arrow" />
		<div v-if="visible">
			<include :src="popoverUrl"/>
		</div>
		<slot />
	</div>
</div>
`,
	/*
	1. If you add tabindex = "- 1" for 'toggle', then you can close it by 'blur'

	2. You can add a close button. It can be any element with a 'close-dropdown' attribute.
		For expample: <span class="close" close-dropdown style="float:right">x</span >
	*/

	data() {
		return {
			state: 'hidden',
			hoverstate : false,
			popoverUrl: ''
		};
	},
	props: {
		icon: String,
		url: String,
		content: [String, Number],
		title: String,
		width: String,
		top: String,
		hover: Boolean
	},
	computed: {
		hasIcon() {
			return !!this.icon;
		},
		iconClass() {
			let cls = "ico po-ico";
			if (this.icon)
				cls += ' ico-' + this.icon;
			return cls;
		},
		visible() {
			return this.url && this.state === 'shown';
		},
		isShowHover() {
			return this.hover && this.hoverstate ? 'show' : undefined;
		}
	},
	methods: {
		mouseover() {
			if (this.hover)
				this.hoverstate = true;
		},
		mouseout() {
			if (this.hover) {
				this.hoverstate = false;
				/*
				setTimeout(x => {
					this.hoverstate = false;
				}, 250);
				*/
			}
		}
	},
	mounted() {
		this.$el._show = () => {
			this.state = 'shown';
			if (this.url)
				this.popoverUrl = '/_popup' + this.url;
		};
		this.$el._hide = () => {
			this.state = 'hidden';
			this.popoverUrl = '';
		};
	}
});
