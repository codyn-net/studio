require 'components/gridobject'
require 'cairo'
require 'mathcontext'

module Cpg::Components
	class State < SimulatedObject
		property :display

		def initialize
			super

			@patouter = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patouter.add_color_stop_rgb(0, 127 / 255.0, 200 / 255.0, 127 / 255.0)
			@patouter.add_color_stop_rgb(@allocation.height, 1, 1, 1)

			@patinner = Cairo::LinearPattern.new(0, 1, 0, 0)
			@patinner.add_color_stop_rgb(0, 193 / 255.0, 255 / 255.0, 217 / 255.0)
			@patinner.add_color_stop_rgb(@allocation.height * 0.8, 1, 1, 1)
		end
		
		def set_property(prop, val)
			super
			
			if prop.to_sym == :display
				request_redraw
			end
		end
		
		def display_s
			v = Cpg::MathContext.new(Cpg::Simulation.instance.state).eval(@display.to_s)
			
			if v.is_a?(Float)
				format('%.2f', v)
			else
				v.to_s
			end
		end

		def draw(ct)
			ct.save
			uw = ct.line_width
		
			ct.set_source(@patouter)
		
			ct.rectangle(0, 0, allocation.width, allocation.height)
			ct.fill
		
			ct.line_width = uw * 2
			ct.set_source_rgb(26 / 255.0, 80 / 255.0, 130 / 255.0)
		
			off = uw * 2 + allocation.width * 0.1
		
			ct.rectangle(off, off, allocation.width - off * 2, allocation.height - off * 2)
			ct.stroke_preserve
			ct.set_source(@patinner)
			ct.fill
			
			ds = display_s
			
			if !ds.empty?
				e = ct.text_extents(ds)
				ct.set_source_rgb(0, 0, 0)
				ct.move_to((allocation.width - e.width) / 2.0, (allocation.height + e.height) / 2.0)
				ct.show_text(ds)
			end
			
			ct.restore
		
			super
		end
	end
end
