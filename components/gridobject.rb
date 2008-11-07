require 'allocation'
require 'glib2'
require 'serialize/object'
require 'range'

module Cpg::Components
	class GridObject < GLib::Object
		type_register

		include Cpg::Serialize::Object
	
		signal_new('request_redraw',
			GLib::Signal::RUN_LAST,
			nil,
			nil)
	
		signal_new('property_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('property_removed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('property_added',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)

		signal_new('range_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)
		
		signal_new('initial_changed',
			GLib::Signal::RUN_LAST,
			nil,
			nil,
			String)

		attr_accessor :allocation, :selected, :links, :mousein, :focus
		property :id, :initial, :range
		read_only :initial, :range
		invisible :initial, :range

		def initialize
			super
			
			@allocation = Cpg::Allocation.new
			@selected = false
			@mousein = false
			@focus = false
			@links = []

			self.initial = Cpg::Serialize::Hash.new
			self.range = Cpg::Serialize::Hash.new
		end
	
		def mouse_enter
			@mousein = true
		end
		
		def mouse_exit
			@mousein = false
		end
	
		def draw_selection(ct)
			alpha = 0.2
	
			ct.rectangle(0, 0, allocation.width, allocation.height)
			ct.set_source_rgba(0, 0, 1, alpha)
	
			ct.fill_preserve
	
			ct.set_source_rgba(0, 0, 0, alpha)
			ct.stroke
		end
		
		def draw_focus(ct)
			uw = ct.line_width
			ct.line_width *= 0.5
			
			fw = 8
			dw = 2
			
			w, h = [allocation.width, allocation.height]
			
			ct.set_source_rgb(0, 0, 0)
			ct.move_to(-uw * dw, uw * fw)
			ct.line_to(-uw * dw, -uw * dw)
			ct.line_to(uw * fw, -uw * dw)
			ct.stroke
			
			ct.move_to(w - uw * fw, -uw * dw)
			ct.line_to(w + uw * dw, -uw * dw)
			ct.line_to(w + uw * dw, uw * fw)
			ct.stroke
			
			ct.move_to(w + uw * dw, h - uw * fw)
			ct.line_to(w + uw * dw, h + uw * dw)
			ct.line_to(w - uw * fw, h + uw * dw)
			ct.stroke
			
			ct.move_to(uw * fw, h + uw * dw)
			ct.line_to(-uw * dw, h + uw * dw)
			ct.line_to(-uw * dw, h - uw * fw)
			ct.stroke
		end
	
		def draw(ct)
			s = to_s
		
			if s && !s.empty?
				e = ct.text_extents(s)
			
				ct.set_source_rgb(0, 0, 0)
				ct.move_to((allocation.width - e.width) / 2.0, allocation.height + e.height * 2)
				ct.show_text(s)
			end

			draw_selection(ct) if @selected
			draw_focus(ct) if @focus
		end
	
		def animate(ts)
			false
		end
	
		def hittest(rect)
			true
		end
	
		def link(l)
			@links << l
		end
	
		def unlink(l)
			@links.delete(l)
		end
	
		def state
			properties.dup
		end

		def add_property(prop)
			if super
				signal_emit('property_added', prop)
				true
			else
				false
			end
		end
	
		def set_property(prop, val)
			if super
				signal_emit('property_changed', prop)
				true
			else
				false
			end
		end
	
		def remove_property(prop)
			if super
				signal_emit('property_removed', prop)

				self.initial.delete(prop)
				self.range.delete(prop)
				return true
			end
		
			false
		end
		
		def initial_value(prop)
			self.initial.get_property(prop)
		end
	
		def set_initial_value(prop, val)
			self.initial.set_property(prop, val)

			signal_emit('initial_changed', prop)
		end
	
		def get_range(prop)
			v = self.range.get_property(prop)
			v && !v.to_s.empty? ? Cpg::Range.new(v) : nil
		end

		def set_range(prop, val)
			if val && !val.to_s.empty?
				val = Cpg::Range.normalize(val.to_s)
			else
				val = ''
			end

			self.range.set_property(prop, val)
			signal_emit('range_changed', prop)
		end
		
		# simulation
		def simulation_reset
			self.initial.each do |k, v|
				next unless v && !v.to_s.empty?
				set_property(k, v) unless get_property(k).to_s == v.to_s
			end
		end
		
		def simulation_update(s)
		end
	
		def simulation_step(dt)
			{}
		end
		
		def signal_do_range_changed(prop)
		end
		
		def signal_do_initial_changed(prop)
		end
	
		def request_redraw
			signal_emit('request_redraw')
		end
	
		def signal_do_request_redraw
		end
	
		def signal_do_property_changed(prop)
			request_redraw if prop == 'id'
		end
	
		def signal_do_property_removed(prop)
		end
		
		def signal_do_property_added(prop)
		end
	end
end
